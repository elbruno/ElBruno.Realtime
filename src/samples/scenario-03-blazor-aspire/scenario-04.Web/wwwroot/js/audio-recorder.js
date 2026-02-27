// Voice conversation using the browser Web Speech API.
// Supports two modes:
//   1. Push-to-talk: click mic → speak → auto-send on final result
//   2. Speak Mode (always-on): mic stays open, auto-sends on each pause,
//      auto-restarts listening after AI finishes speaking.
//
// SpeechRecognition for STT, SpeechSynthesis for TTS.
window.voiceChat = {
    recognition: null,
    dotNetRef: null,
    isListening: false,
    autoSpeak: true,
    speakMode: false,      // always-on conversation mode
    _speakModeRestart: false, // internal: should we restart after onend?
    _isSpeaking: false,    // true while TTS is playing

    // Check if the browser supports speech recognition
    isSupported: function () {
        return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
    },

    // Start listening for speech input (push-to-talk mode)
    start: function (dotNetRef) {
        this.dotNetRef = dotNetRef;
        this.speakMode = false;
        this._speakModeRestart = false;
        this._startRecognition();
    },

    // Enter always-on Speak Mode
    startSpeakMode: function (dotNetRef) {
        if (!this.isSupported()) {
            dotNetRef.invokeMethodAsync('OnSpeechError',
                'Speech recognition is not supported in this browser. Use Chrome or Edge.');
            return;
        }
        this.dotNetRef = dotNetRef;
        this.speakMode = true;
        this._speakModeRestart = true;
        this._startRecognition();
    },

    // Exit Speak Mode
    stopSpeakMode: function () {
        this.speakMode = false;
        this._speakModeRestart = false;
        this.stop();
        // Cancel any ongoing TTS
        window.speechSynthesis.cancel();
        this._isSpeaking = false;
    },

    _startRecognition: function () {
        if (!this.isSupported()) {
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnSpeechError',
                    'Speech recognition is not supported in this browser. Use Chrome or Edge.');
            }
            return;
        }

        // Stop existing recognition if running
        if (this.recognition && this.isListening) {
            this._speakModeRestart = this.speakMode; // preserve restart intent
            this.recognition.abort();
        }

        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        this.recognition = new SpeechRecognition();
        this.recognition.continuous = this.speakMode;
        this.recognition.interimResults = true;
        this.recognition.lang = 'en-US';
        this.recognition.maxAlternatives = 1;

        this.recognition.onstart = () => {
            this.isListening = true;
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnListeningStateChanged', true);
            }
        };

        this.recognition.onresult = (event) => {
            let interimTranscript = '';
            let finalTranscript = '';

            for (let i = event.resultIndex; i < event.results.length; i++) {
                const transcript = event.results[i][0].transcript;
                if (event.results[i].isFinal) {
                    finalTranscript += transcript;
                } else {
                    interimTranscript += transcript;
                }
            }

            // Send interim results for live preview in the input field
            if (interimTranscript && this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnInterimSpeechResult', interimTranscript);
            }

            // Send final result to trigger the message send
            if (finalTranscript && this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnFinalSpeechResult', finalTranscript);
            }
        };

        this.recognition.onerror = (event) => {
            let errorMsg = 'Speech recognition error';
            switch (event.error) {
                case 'no-speech':
                    // In speak mode, no-speech is normal — just restart
                    if (this.speakMode) {
                        return;
                    }
                    errorMsg = 'No speech detected. Try again.';
                    break;
                case 'audio-capture':
                    errorMsg = 'No microphone found. Check your audio settings.';
                    this._speakModeRestart = false;
                    break;
                case 'not-allowed':
                    errorMsg = 'Microphone access denied. Please allow microphone access in your browser settings.';
                    this._speakModeRestart = false;
                    break;
                case 'aborted':
                    // Intentional abort, don't report error
                    return;
                case 'network':
                    errorMsg = 'Network error during speech recognition.';
                    break;
                default:
                    errorMsg = 'Speech recognition error: ' + event.error;
            }
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnSpeechError', errorMsg);
            }
        };

        this.recognition.onend = () => {
            this.isListening = false;
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnListeningStateChanged', false);
            }

            // In Speak Mode, auto-restart after a short delay (unless stopped)
            if (this.speakMode && this._speakModeRestart) {
                setTimeout(() => {
                    if (this.speakMode && this._speakModeRestart && !this._isSpeaking) {
                        this._startRecognition();
                    }
                }, 300);
            } else if (!this.speakMode && this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnSpeechEnded');
            }
        };

        this.recognition.start();
    },

    // Stop listening
    stop: function () {
        if (this.recognition && this.isListening) {
            this.recognition.stop();
            this.isListening = false;
        }
    },

    // Speak text aloud using browser TTS.
    // In Speak Mode, pauses recognition while speaking to avoid echo,
    // then auto-restarts when done.
    speak: function (text) {
        if (!this.autoSpeak) return;

        // Cancel any ongoing speech
        window.speechSynthesis.cancel();

        const utterance = new SpeechSynthesisUtterance(text);
        utterance.rate = 1.0;
        utterance.pitch = 1.0;
        utterance.volume = 1.0;
        utterance.lang = 'en-US';

        if (this.speakMode) {
            // Pause recognition while TTS is playing to avoid feedback loop
            this._isSpeaking = true;
            if (this.recognition && this.isListening) {
                this.recognition.stop();
            }

            utterance.onend = () => {
                this._isSpeaking = false;
                // Resume listening after TTS finishes
                if (this.speakMode && this._speakModeRestart) {
                    setTimeout(() => this._startRecognition(), 200);
                }
            };

            utterance.onerror = () => {
                this._isSpeaking = false;
                if (this.speakMode && this._speakModeRestart) {
                    setTimeout(() => this._startRecognition(), 200);
                }
            };
        }

        window.speechSynthesis.speak(utterance);
    },

    // Stop any ongoing TTS playback (barge-in)
    stopSpeaking: function () {
        window.speechSynthesis.cancel();
        this._isSpeaking = false;
        // Resume listening immediately after barge-in
        if (this.speakMode && this._speakModeRestart && !this.isListening) {
            this._startRecognition();
        }
    },

    // Toggle auto-speak on/off
    setAutoSpeak: function (enabled) {
        this.autoSpeak = enabled;
        if (!enabled) {
            window.speechSynthesis.cancel();
            this._isSpeaking = false;
        }
    }
};
