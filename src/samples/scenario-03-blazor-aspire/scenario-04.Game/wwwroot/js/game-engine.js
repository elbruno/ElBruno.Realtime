const COLORS = {
    skyTop: '#2c1654',
    skyBottom: '#7c3c8c',
    groundTop: '#5a8f3e',
    groundDirt: '#3a2820',
    player: '#FF6B9D',
    playerSkin: '#FFA07A',
    enemy: '#9D4EDD',
    rock: '#6c757d',
    projectile: '#FFD60A',
    cloud: 'rgba(255, 255, 255, 0.3)',
    sun: '#FFE66D',
    particle: '#FF6B9D',
    groundMarker: '#4a7c32'
};

const CONFIG = {
    gravity: 0.5,
    jumpVelocity: -12,
    groundHeight: 80,
    baseSpeed: 2,
    speedIncrease: 0.4,
    speedInterval: 30,
    scoreInterval: 5,
    shootCooldown: 0.5,
    invincibleSeconds: 2
};

let canvas;
let ctx;
let dotNetRef;
let rafId;
let initialized = false;

const state = {
    running: false,
    gameOver: false,
    time: 0,
    score: 0,
    lives: 3,
    scrollSpeed: CONFIG.baseSpeed,
    nextSpeedTime: CONFIG.speedInterval,
    nextScoreTime: CONFIG.scoreInterval,
    invincibleUntil: 0,
    screenShake: 0,
    worldOffset: 0
};

const player = {
    x: 0,
    y: 0,
    width: 32,
    height: 48,
    vy: 0,
    onGround: true,
    animFrame: 0,
    jumpHeld: false
};

let obstacles = [];
let holes = [];
let enemies = [];
let projectiles = [];
let particles = [];
let floatingTexts = [];
let groundMarkers = [];
let clouds = [];
let obstacleTimer = 1.4;
let enemyTimer = 2.5;
let shootCooldown = 0;
let lastSpeechText = '';
let lastActionText = '';
let speechTextTimer = 0;
let actionTextTimer = 0;

const keysDown = new Set();

const voice = {
    recognition: null,
    dotNetRef: null,
    isListening: false,
    _restart: false,
    _isSpeaking: false
};

export function initGame(canvasId, ref) {
    canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Game engine init failed: canvas not found for id', canvasId);
        return;
    }
    ctx = canvas.getContext('2d');
    dotNetRef = ref;
    player.x = Math.floor(canvas.width * 0.25);
    resetGame();

    if (!initialized) {
        attachInput();
        initClouds();
        initGroundMarkers();
        initialized = true;
    }

    // Focus the game area wrapper so keyboard events work immediately
    const gameArea = canvas.closest('.game-area') || canvas.parentElement;
    if (gameArea) {
        gameArea.focus();
    }

    console.log(`Game engine initialized, canvas: ${canvas.width}x${canvas.height}`);
}

export function startGame() {
    if (!canvas || state.running || state.gameOver) {
        return;
    }
    state.running = true;
    state.lastTime = performance.now();
    rafId = requestAnimationFrame(loop);
    focusGameArea();
    console.log('Game started');
}

export function pauseGame() {
    state.running = false;
    if (rafId) {
        cancelAnimationFrame(rafId);
    }
}

export function resetGame() {
    state.running = false;
    state.gameOver = false;
    state.time = 0;
    state.score = 0;
    state.lives = 3;
    state.scrollSpeed = CONFIG.baseSpeed;
    state.nextSpeedTime = CONFIG.speedInterval;
    state.nextScoreTime = CONFIG.scoreInterval;
    state.invincibleUntil = 0;
    state.screenShake = 0;
    state.worldOffset = 0;

    player.y = getGroundY() - player.height;
    player.vy = 0;
    player.onGround = true;
    player.animFrame = 0;
    player.jumpHeld = false;

    obstacles = [];
    holes = [];
    enemies = [];
    projectiles = [];
    particles = [];
    floatingTexts = [];
    obstacleTimer = 1.4;
    enemyTimer = 2.5;
    shootCooldown = 0;
    lastSpeechText = '';
    lastActionText = '';
    speechTextTimer = 0;
    actionTextTimer = 0;

    initGroundMarkers();

    notifyScore();
    notifyLives();
    render();
    focusGameArea();
    console.log('Game reset');
}

export function applyVoiceCommand(command) {
    if (!command) {
        return;
    }
    console.log('Applying voice command:', command);
    const normalized = command.toLowerCase();
    let executed = false;
    if (normalized === 'jump') {
        executed = tryJump(true);
        if (executed) {
            showActionText('ACTION: JUMP');
        }
    } else if (normalized === 'shoot') {
        executed = tryShoot(true);
        if (executed) {
            showActionText('ACTION: SHOOT');
        }
    }
    if (executed) {
        addScore(50);
        emitEvent('voice-command', normalized);
    }
}

export function showSpeechText(text) {
    lastSpeechText = text;
    speechTextTimer = 3.0;
}

function showActionText(text) {
    lastActionText = text;
    actionTextTimer = 2.0;
}

export function isVoiceSupported() {
    return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
}

export function startVoiceControl(ref) {
    console.log('Voice control starting...');
    voice.dotNetRef = ref;
    if (!isVoiceSupported()) {
        console.warn('Speech recognition not supported in this browser');
        if (voice.dotNetRef) {
            voice.dotNetRef.invokeMethodAsync('OnVoiceError', 'Speech recognition is not supported. Use Chrome or Edge.');
        }
        return;
    }
    voice._restart = true;
    startRecognition();
}

export function stopVoiceControl() {
    voice._restart = false;
    if (voice.recognition && voice.isListening) {
        voice.recognition.stop();
    }
}

export function speakText(text) {
    if (!text) {
        return;
    }
    console.log('Speaking:', text);
    showSpeechText(text);
    window.speechSynthesis.cancel();
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.rate = 1.0;
    utterance.pitch = 1.0;
    utterance.volume = 1.0;
    utterance.lang = 'en-US';

    if (voice.isListening) {
        voice._isSpeaking = true;
        if (voice.recognition) {
            voice.recognition.stop();
        }
        utterance.onend = () => {
            voice._isSpeaking = false;
            if (voice._restart) {
                setTimeout(() => startRecognition(), 200);
            }
        };
        utterance.onerror = () => {
            voice._isSpeaking = false;
            if (voice._restart) {
                setTimeout(() => startRecognition(), 200);
            }
        };
    }
    window.speechSynthesis.speak(utterance);
}

function startRecognition() {
    console.log('Starting speech recognition...');
    if (!isVoiceSupported()) {
        return;
    }
    if (voice.recognition && voice.isListening) {
        voice.recognition.abort();
    }

    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    voice.recognition = new SpeechRecognition();
    voice.recognition.continuous = true;
    voice.recognition.interimResults = true;
    voice.recognition.lang = 'en-US';
    voice.recognition.maxAlternatives = 1;

    voice.recognition.onstart = () => {
        console.log('Speech recognition started');
        voice.isListening = true;
        if (voice.dotNetRef) {
            voice.dotNetRef.invokeMethodAsync('OnVoiceStatusChanged', true);
        }
    };

    voice.recognition.onresult = (event) => {
        let finalTranscript = '';
        for (let i = event.resultIndex; i < event.results.length; i++) {
            if (event.results[i].isFinal) {
                finalTranscript += event.results[i][0].transcript;
            }
        }
        if (finalTranscript) {
            const command = matchVoiceCommand(finalTranscript);
            console.log('Voice transcript:', finalTranscript, 'Matched command:', command);
            if (command) {
                applyVoiceCommand(command);
                if (voice.dotNetRef) {
                    voice.dotNetRef.invokeMethodAsync('OnVoiceCommand', command);
                }
            }
        }
    };

    voice.recognition.onerror = (event) => {
        console.error('Speech recognition error:', event.error, event);
        let errorMsg = 'Speech recognition error';
        switch (event.error) {
            case 'no-speech':
                return;
            case 'audio-capture':
                errorMsg = 'No microphone found. Check your audio settings.';
                voice._restart = false;
                break;
            case 'not-allowed':
                errorMsg = 'Microphone access denied.';
                voice._restart = false;
                break;
            case 'aborted':
                return;
            case 'network':
                errorMsg = 'Network error during speech recognition. This may be because the browser needs HTTPS or internet access for cloud speech recognition.';
                break;
            default:
                errorMsg = `Speech recognition error (${event.error}): ${event.message || 'Unknown cause'}`;
        }
        if (voice.dotNetRef) {
            voice.dotNetRef.invokeMethodAsync('OnVoiceError', errorMsg);
        }
    };

    voice.recognition.onend = () => {
        console.log('Speech recognition ended, will restart:', voice._restart);
        voice.isListening = false;
        if (voice.dotNetRef) {
            voice.dotNetRef.invokeMethodAsync('OnVoiceStatusChanged', false);
        }
        if (voice._restart && !voice._isSpeaking) {
            setTimeout(() => startRecognition(), 300);
        }
    };

    voice.recognition.start();
}

function matchVoiceCommand(transcript) {
    const text = transcript.toLowerCase();
    const words = text.split(/\s+/);
    console.log('Matching voice:', text, 'Words:', words);
    const commands = {
        jump: ['jump', 'up', 'hop', 'leap'],
        shoot: ['shoot', 'fire', 'bang', 'pew', 'attack', 'hit']
    };
    for (const [command, aliases] of Object.entries(commands)) {
        if (aliases.some(alias => words.includes(alias))) {
            return command;
        }
    }
    return null;
}

function attachInput() {
    console.log('Keyboard input attached');
    window.addEventListener('keydown', (event) => {
        if (keysDown.has(event.code)) {
            return;
        }
        keysDown.add(event.code);
        console.log('Key pressed:', event.code);

        // Refocus game area so buttons don't steal input
        if (event.target && (event.target.tagName === 'BUTTON' || event.target.tagName === 'INPUT' || event.target.tagName === 'SELECT')) {
            focusGameArea();
        }

        if (event.code === 'Space' || event.code === 'ArrowUp') {
            event.preventDefault();
            tryJump(false);
        }
        if (event.code === 'KeyS') {
            event.preventDefault();
            tryShoot(false);
        }

        notifyDebugKey(event.code);
    });

    window.addEventListener('keyup', (event) => {
        keysDown.delete(event.code);
        if (event.code === 'Space' || event.code === 'ArrowUp') {
            player.jumpHeld = false;
        }
    });
}

function focusGameArea() {
    if (!canvas) return;
    const gameArea = document.getElementById('game-area');
    if (gameArea) {
        gameArea.focus();
    }
}

function notifyDebugKey(code) {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnDebugKeyEvent', code);
    }
}

function loop(timestamp) {
    if (!state.running) {
        return;
    }
    const delta = (timestamp - state.lastTime) / 1000;
    state.lastTime = timestamp;
    update(delta);
    render();
    rafId = requestAnimationFrame(loop);
}

function update(delta) {
    if (state.gameOver) {
        return;
    }
    state.time += delta;
    const frame = delta * 60;
    const scroll = state.scrollSpeed * frame;
    shootCooldown = Math.max(0, shootCooldown - delta);

    if (state.time >= state.nextSpeedTime) {
        state.scrollSpeed += CONFIG.speedIncrease;
        state.nextSpeedTime += CONFIG.speedInterval;
        emitEvent('speed-up', state.scrollSpeed.toFixed(1));
    }

    if (state.time >= state.nextScoreTime) {
        addScore(5);
        state.nextScoreTime += CONFIG.scoreInterval;
    }

    obstacleTimer -= delta;
    if (obstacleTimer <= 0) {
        if (Math.random() < 0.35) {
            spawnHole();
        } else {
            spawnRock();
        }
        obstacleTimer = randomRange(0.9, 1.8) / Math.max(0.6, state.scrollSpeed / 2);
    }

    enemyTimer -= delta;
    if (enemyTimer <= 0) {
        spawnEnemy();
        enemyTimer = randomRange(1.8, 3.4) / Math.max(0.7, state.scrollSpeed / 2);
    }

    // Variable jump: extra gravity when space released while ascending (gravity-cut)
    if (!player.jumpHeld && player.vy < 0) {
        player.vy += CONFIG.gravity * 2 * frame;
    }

    player.vy += CONFIG.gravity * frame;
    player.y += player.vy * frame;

    const groundY = getGroundY();
    const overHole = isOverHole(player.x + player.width * 0.5);

    if (!overHole && player.y + player.height >= groundY) {
        player.y = groundY - player.height;
        player.vy = 0;
        player.onGround = true;
        player.jumpHeld = false;
    } else if (overHole && player.y + player.height >= groundY) {
        player.onGround = false;
    }

    if (player.y > canvas.height + 120) {
        loseLife('hole');
    }

    obstacles.forEach(obstacle => {
        obstacle.x -= scroll;
    });
    obstacles = obstacles.filter(obstacle => obstacle.x + obstacle.width > -80);

    holes.forEach(hole => {
        hole.x -= scroll;
    });
    holes = holes.filter(hole => hole.x + hole.width > -120);

    enemies.forEach(enemy => {
        enemy.x -= (enemy.speed * frame);
        enemy.animFrame = (enemy.animFrame || 0) + delta * 8;
    });
    enemies = enemies.filter(enemy => enemy.x + enemy.width > -80);

    projectiles.forEach(projectile => {
        projectile.x += projectile.speed * frame;
        projectile.life = (projectile.life || 0) + delta;
    });
    projectiles = projectiles.filter(projectile => projectile.x < canvas.width + 120);

    particles.forEach(particle => {
        particle.x += particle.vx * frame;
        particle.y += particle.vy * frame;
        particle.vy += 0.2 * frame;
        particle.life -= delta;
        particle.alpha = Math.max(0, particle.life / particle.maxLife);
    });
    particles = particles.filter(p => p.life > 0);

    floatingTexts.forEach(text => {
        text.y -= 0.5 * frame;
        text.life -= delta;
        text.alpha = Math.max(0, text.life / text.maxLife);
    });
    floatingTexts = floatingTexts.filter(t => t.life > 0);

    groundMarkers.forEach(marker => {
        marker.x -= scroll;
    });
    groundMarkers = groundMarkers.filter(m => m.x > -50);
    while (groundMarkers.length < 50) {
        const lastX = groundMarkers.length > 0 ? groundMarkers[groundMarkers.length - 1].x : 0;
        groundMarkers.push({
            x: lastX + randomRange(30, 80),
            type: Math.floor(Math.random() * 3)
        });
    }

    clouds.forEach(cloud => {
        cloud.x -= scroll * 0.2;
        if (cloud.x + cloud.width < 0) {
            cloud.x = canvas.width + randomRange(50, 200);
        }
    });

    state.worldOffset += scroll;
    state.screenShake = Math.max(0, state.screenShake - delta * 8);

    if (player.onGround) {
        player.animFrame += delta * 10;
    }

    speechTextTimer = Math.max(0, speechTextTimer - delta);
    actionTextTimer = Math.max(0, actionTextTimer - delta);

    checkCollisions();
    checkObstacleClears();
}

function render() {
    if (!ctx || !canvas) {
        return;
    }
    const groundY = getGroundY();
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.save();
    if (state.screenShake > 0) {
        const shakeX = (Math.random() - 0.5) * state.screenShake * 10;
        const shakeY = (Math.random() - 0.5) * state.screenShake * 10;
        ctx.translate(shakeX, shakeY);
    }

    const gradient = ctx.createLinearGradient(0, 0, 0, canvas.height);
    gradient.addColorStop(0, COLORS.skyTop);
    gradient.addColorStop(1, COLORS.skyBottom);
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = COLORS.sun;
    ctx.beginPath();
    ctx.arc(canvas.width - 80, 60, 30, 0, Math.PI * 2);
    ctx.fill();

    clouds.forEach(cloud => {
        ctx.fillStyle = COLORS.cloud;
        ctx.beginPath();
        ctx.ellipse(cloud.x, cloud.y, cloud.width, cloud.height, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.ellipse(cloud.x + 20, cloud.y - 5, cloud.width * 0.8, cloud.height * 0.8, 0, 0, Math.PI * 2);
        ctx.fill();
        ctx.beginPath();
        ctx.ellipse(cloud.x - 15, cloud.y + 3, cloud.width * 0.6, cloud.height * 0.6, 0, 0, Math.PI * 2);
        ctx.fill();
    });

    ctx.fillStyle = COLORS.groundTop;
    ctx.fillRect(0, groundY, canvas.width, 8);
    ctx.fillStyle = COLORS.groundDirt;
    ctx.fillRect(0, groundY + 8, canvas.width, CONFIG.groundHeight - 8);

    for (let i = 0; i < canvas.width; i += 40) {
        const offset = Math.floor(state.worldOffset) % 40;
        ctx.strokeStyle = COLORS.groundDirt;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(i - offset, groundY + 20);
        ctx.lineTo(i - offset + 30, groundY + 25);
        ctx.stroke();
    }

    groundMarkers.forEach(marker => {
        if (marker.x < 0 || marker.x > canvas.width) return;
        ctx.fillStyle = COLORS.groundMarker;
        if (marker.type === 0) {
            ctx.fillRect(marker.x, groundY + 2, 3, 4);
            ctx.fillRect(marker.x - 2, groundY + 4, 2, 2);
            ctx.fillRect(marker.x + 3, groundY + 4, 2, 2);
        } else if (marker.type === 1) {
            ctx.beginPath();
            ctx.arc(marker.x, groundY + 4, 2, 0, Math.PI * 2);
            ctx.fill();
        } else {
            ctx.fillRect(marker.x, groundY + 2, 6, 2);
        }
    });

    holes.forEach(hole => {
        const holeGradient = ctx.createLinearGradient(hole.x, groundY, hole.x, groundY + CONFIG.groundHeight);
        holeGradient.addColorStop(0, 'rgba(0, 0, 0, 0.5)');
        holeGradient.addColorStop(1, 'rgba(0, 0, 0, 0.8)');
        ctx.fillStyle = holeGradient;
        ctx.fillRect(hole.x, groundY, hole.width, CONFIG.groundHeight);
        
        ctx.strokeStyle = COLORS.groundDirt;
        ctx.lineWidth = 3;
        ctx.beginPath();
        ctx.moveTo(hole.x, groundY);
        ctx.lineTo(hole.x, groundY + CONFIG.groundHeight);
        ctx.stroke();
        ctx.beginPath();
        ctx.moveTo(hole.x + hole.width, groundY);
        ctx.lineTo(hole.x + hole.width, groundY + CONFIG.groundHeight);
        ctx.stroke();
    });

    obstacles.forEach(obstacle => {
        drawRock(obstacle);
    });

    enemies.forEach(enemy => {
        drawEnemy(enemy);
    });

    projectiles.forEach(projectile => {
        drawProjectile(projectile);
    });

    particles.forEach(particle => {
        ctx.globalAlpha = particle.alpha;
        ctx.fillStyle = particle.color;
        ctx.fillRect(particle.x - 2, particle.y - 2, 4, 4);
        ctx.globalAlpha = 1;
    });

    floatingTexts.forEach(text => {
        ctx.globalAlpha = text.alpha;
        ctx.font = 'bold 18px monospace';
        ctx.fillStyle = text.color;
        ctx.textAlign = 'center';
        ctx.fillText(text.text, text.x, text.y);
        ctx.globalAlpha = 1;
    });

    const isInvincible = isInvincibleNow();
    const blink = isInvincible && Math.floor(state.time * 10) % 2 === 0;
    if (!blink) {
        drawPlayer();
    }

    if (speechTextTimer > 0) {
        const alpha = Math.min(1, speechTextTimer / 0.5);
        ctx.globalAlpha = alpha;
        ctx.font = 'bold 16px sans-serif';
        ctx.fillStyle = '#ffffff';
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 3;
        ctx.textAlign = 'left';
        const bubbleX = player.x + player.width + 10;
        const bubbleY = player.y - 10;
        ctx.strokeText(lastSpeechText, bubbleX, bubbleY);
        ctx.fillText(lastSpeechText, bubbleX, bubbleY);
        ctx.globalAlpha = 1;
    }

    if (actionTextTimer > 0) {
        const alpha = Math.min(1, actionTextTimer / 0.5);
        ctx.globalAlpha = alpha;
        ctx.font = 'bold 24px monospace';
        ctx.fillStyle = '#FFD60A';
        ctx.strokeStyle = '#000000';
        ctx.lineWidth = 4;
        ctx.textAlign = 'center';
        ctx.strokeText(lastActionText, canvas.width / 2, 60);
        ctx.fillText(lastActionText, canvas.width / 2, 60);
        ctx.globalAlpha = 1;
    }

    ctx.restore();
}

function drawPlayer() {
    const legOffset = Math.sin(player.animFrame) * 4;
    
    ctx.fillStyle = COLORS.player;
    ctx.fillRect(player.x + 8, player.y + 12, 16, 24);
    
    ctx.fillStyle = COLORS.playerSkin;
    ctx.beginPath();
    ctx.arc(player.x + 16, player.y + 8, 8, 0, Math.PI * 2);
    ctx.fill();
    
    ctx.fillStyle = '#000000';
    ctx.fillRect(player.x + 12, player.y + 6, 3, 3);
    ctx.fillRect(player.x + 19, player.y + 6, 3, 3);
    
    ctx.fillStyle = COLORS.player;
    ctx.fillRect(player.x + 14, player.y + 2, 4, 4);
    
    ctx.fillStyle = COLORS.playerSkin;
    ctx.fillRect(player.x + 4, player.y + 16, 6, 2);
    ctx.fillRect(player.x + 22, player.y + 16, 6, 2);
    
    ctx.fillStyle = COLORS.player;
    ctx.fillRect(player.x + 10, player.y + 36 + (player.onGround ? legOffset : 0), 5, 12);
    ctx.fillRect(player.x + 17, player.y + 36 + (player.onGround ? -legOffset : 0), 5, 12);
}

function drawRock(rock) {
    ctx.fillStyle = COLORS.rock;
    ctx.beginPath();
    ctx.moveTo(rock.x + 4, rock.y + rock.height);
    ctx.lineTo(rock.x + rock.width * 0.2, rock.y);
    ctx.lineTo(rock.x + rock.width * 0.6, rock.y + rock.height * 0.15);
    ctx.lineTo(rock.x + rock.width - 2, rock.y + rock.height * 0.3);
    ctx.lineTo(rock.x + rock.width, rock.y + rock.height);
    ctx.closePath();
    ctx.fill();
    
    ctx.strokeStyle = 'rgba(0, 0, 0, 0.3)';
    ctx.lineWidth = 2;
    ctx.beginPath();
    ctx.moveTo(rock.x + rock.width * 0.3, rock.y + rock.height * 0.2);
    ctx.lineTo(rock.x + rock.width * 0.7, rock.y + rock.height * 0.6);
    ctx.stroke();
    
    ctx.fillStyle = 'rgba(0, 0, 0, 0.2)';
    ctx.fillRect(rock.x + rock.width * 0.6, rock.y + rock.height * 0.4, rock.width * 0.4, rock.height * 0.6);
}

function drawEnemy(enemy) {
    const bounce = Math.sin((enemy.animFrame || 0)) * 3;
    
    ctx.fillStyle = COLORS.enemy;
    ctx.fillRect(enemy.x, enemy.y + bounce, enemy.width, enemy.height - bounce);
    
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(enemy.x + 8, enemy.y + 10 + bounce, 4, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(enemy.x + 20, enemy.y + 10 + bounce, 4, 0, Math.PI * 2);
    ctx.fill();
    
    ctx.fillStyle = '#000000';
    ctx.beginPath();
    ctx.arc(enemy.x + 8, enemy.y + 10 + bounce, 2, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(enemy.x + 20, enemy.y + 10 + bounce, 2, 0, Math.PI * 2);
    ctx.fill();
    
    ctx.fillStyle = COLORS.enemy;
    ctx.beginPath();
    ctx.moveTo(enemy.x + 4, enemy.y + bounce);
    ctx.lineTo(enemy.x + 8, enemy.y - 4 + bounce);
    ctx.lineTo(enemy.x + 12, enemy.y + bounce);
    ctx.fill();
    ctx.beginPath();
    ctx.moveTo(enemy.x + 18, enemy.y + bounce);
    ctx.lineTo(enemy.x + 22, enemy.y - 4 + bounce);
    ctx.lineTo(enemy.x + 26, enemy.y + bounce);
    ctx.fill();
}

function drawProjectile(projectile) {
    ctx.save();
    ctx.translate(projectile.x + projectile.width / 2, projectile.y + projectile.height / 2);
    
    const pulse = Math.sin((projectile.life || 0) * 20) * 0.3 + 0.7;
    ctx.scale(pulse, pulse);
    
    ctx.fillStyle = COLORS.projectile;
    ctx.shadowColor = COLORS.projectile;
    ctx.shadowBlur = 8;
    ctx.beginPath();
    ctx.arc(0, 0, 6, 0, Math.PI * 2);
    ctx.fill();
    
    ctx.shadowBlur = 0;
    ctx.restore();
}

function initClouds() {
    clouds = [];
    for (let i = 0; i < 5; i++) {
        clouds.push({
            x: Math.random() * canvas.width,
            y: 40 + Math.random() * 100,
            width: 40 + Math.random() * 30,
            height: 15 + Math.random() * 10
        });
    }
}

function initGroundMarkers() {
    groundMarkers = [];
    for (let i = 0; i < 50; i++) {
        groundMarkers.push({
            x: i * 60,
            type: Math.floor(Math.random() * 3)
        });
    }
}

function tryJump(isVoice) {
    console.log(`Jump attempted, onGround: ${player.onGround}, running: ${state.running}`);
    if (!state.running || state.gameOver || !player.onGround) {
        return false;
    }
    player.vy = CONFIG.jumpVelocity;
    player.onGround = false;
    player.jumpHeld = true;
    
    if (isVoice) {
        emitEvent('voice-jump', 'jump');
    }
    
    spawnParticles(player.x + player.width / 2, player.y + player.height, 8, COLORS.groundTop);
    return true;
}

function tryShoot(isVoice) {
    console.log(`Shoot attempted, cooldown: ${shootCooldown.toFixed(2)}, running: ${state.running}`);
    if (!state.running || state.gameOver || shootCooldown > 0) {
        return false;
    }
    shootCooldown = CONFIG.shootCooldown;
    projectiles.push({
        x: player.x + player.width + 4,
        y: player.y + player.height / 2 - 4,
        width: 12,
        height: 6,
        speed: state.scrollSpeed * 2.2 + 3,
        life: 0
    });
    spawnParticles(player.x + player.width, player.y + player.height / 2, 6, COLORS.projectile);
    if (isVoice) {
        emitEvent('voice-shoot', 'shoot');
    }
    return true;
}

function spawnRock() {
    const width = randomRange(28, 60);
    const height = randomRange(28, 52);
    obstacles.push({
        x: canvas.width + randomRange(10, 70),
        y: getGroundY() - height,
        width,
        height,
        passed: false,
        hit: false
    });
}

function spawnHole() {
    holes.push({
        x: canvas.width + randomRange(20, 90),
        width: randomRange(50, 90)
    });
}

function spawnEnemy() {
    enemies.push({
        x: canvas.width + randomRange(30, 120),
        y: getGroundY() - 32,
        width: 30,
        height: 32,
        speed: state.scrollSpeed + randomRange(0.3, 0.8)
    });
}

function checkCollisions() {
    const playerRect = { x: player.x, y: player.y, width: player.width, height: player.height };
    const invincible = isInvincibleNow();

    obstacles.forEach(obstacle => {
        if (!obstacle.hit && intersects(playerRect, obstacle)) {
            if (player.vy > 0 && player.y + player.height - 10 < obstacle.y + 5) {
                player.y = obstacle.y - player.height;
                player.vy = 0;
                player.onGround = true;
                obstacle.stomped = true;
                addFloatingText('+10', obstacle.x + obstacle.width / 2, obstacle.y - 10);
                return;
            }
            obstacle.hit = true;
            if (!invincible) {
                loseLife('hit');
            }
        }
    });

    for (let i = enemies.length - 1; i >= 0; i--) {
        const enemy = enemies[i];
        if (intersects(playerRect, enemy)) {
            if (player.vy > 0 && player.y + player.height - 10 < enemy.y + 5) {
                enemies.splice(i, 1);
                player.vy = -8;
                addScore(25);
                addFloatingText('+25', enemy.x + enemy.width / 2, enemy.y);
                spawnParticles(enemy.x + enemy.width / 2, enemy.y + enemy.height / 2, 15, COLORS.enemy);
                emitEvent('enemy-killed', 'stomp');
                continue;
            }
            if (!invincible) {
                loseLife('enemy');
            }
        }
    }

    for (let e = enemies.length - 1; e >= 0; e -= 1) {
        for (let p = projectiles.length - 1; p >= 0; p -= 1) {
            if (intersects(enemies[e], projectiles[p])) {
                const enemy = enemies[e];
                enemies.splice(e, 1);
                projectiles.splice(p, 1);
                addScore(25);
                addFloatingText('+25', enemy.x + enemy.width / 2, enemy.y);
                spawnParticles(enemy.x + enemy.width / 2, enemy.y + enemy.height / 2, 15, COLORS.enemy);
                emitEvent('enemy-killed', 'projectile');
                break;
            }
        }
    }
}

function checkObstacleClears() {
    const groundY = getGroundY();
    obstacles.forEach(obstacle => {
        if (!obstacle.passed && obstacle.x + obstacle.width < player.x) {
            obstacle.passed = true;
            if (player.y + player.height < groundY - 4 && !obstacle.hit && !obstacle.stomped) {
                addScore(10);
                addFloatingText('+10', obstacle.x + obstacle.width / 2, obstacle.y);
                emitEvent('jumped-obstacle', 'rock');
            }
        }
    });
}

function loseLife(reason) {
    if (isInvincibleNow()) {
        return;
    }
    state.lives -= 1;
    notifyLives();
    emitEvent('player-hit', reason);
    state.screenShake = 1;
    spawnParticles(player.x + player.width / 2, player.y + player.height / 2, 20, COLORS.player);

    if (state.lives <= 0) {
        state.gameOver = true;
        state.running = false;
        emitEvent('game-over', reason);
        speakText(`Game over! Your final score is ${state.score} points.`);
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnGameOver', state.score);
        }
        return;
    }

    state.invincibleUntil = state.time + CONFIG.invincibleSeconds;
    player.y = getGroundY() - player.height;
    player.vy = 0;
    player.onGround = true;
}

function addScore(points) {
    state.score += points;
    notifyScore();
}

function notifyScore() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnScoreChanged', state.score);
    }
}

function notifyLives() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnLivesChanged', state.lives);
    }
}

function emitEvent(type, data) {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnGameEvent', type, data ?? '');
    }
}

function intersects(a, b) {
    return a.x < b.x + b.width &&
        a.x + a.width > b.x &&
        a.y < b.y + b.height &&
        a.y + a.height > b.y;
}

function isOverHole(x) {
    return holes.some(hole => x > hole.x && x < hole.x + hole.width);
}

function isInvincibleNow() {
    return state.time < state.invincibleUntil;
}

function getGroundY() {
    return canvas.height - CONFIG.groundHeight;
}

function randomRange(min, max) {
    return Math.random() * (max - min) + min;
}

function spawnParticles(x, y, count, color) {
    for (let i = 0; i < count; i++) {
        particles.push({
            x,
            y,
            vx: (Math.random() - 0.5) * 4,
            vy: (Math.random() - 0.5) * 4 - 2,
            life: 0.5 + Math.random() * 0.5,
            maxLife: 1,
            alpha: 1,
            color
        });
    }
}

function addFloatingText(text, x, y) {
    floatingTexts.push({
        text,
        x,
        y,
        life: 1.5,
        maxLife: 1.5,
        alpha: 1,
        color: '#FFD60A'
    });
}
