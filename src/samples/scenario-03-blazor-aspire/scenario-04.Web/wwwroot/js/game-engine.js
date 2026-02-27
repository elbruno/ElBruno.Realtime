const COLORS = {
    sky: '#87CEEB',
    ground: '#8B5A2B',
    player: '#2ECC71',
    enemy: '#E74C3C',
    rock: '#7F8C8D',
    projectile: '#F1C40F'
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
    invincibleUntil: 0
};

const player = {
    x: 0,
    y: 0,
    width: 32,
    height: 48,
    vy: 0,
    onGround: true
};

let obstacles = [];
let holes = [];
let enemies = [];
let projectiles = [];
let obstacleTimer = 1.4;
let enemyTimer = 2.5;
let shootCooldown = 0;

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
        return;
    }
    ctx = canvas.getContext('2d');
    dotNetRef = ref;
    player.x = Math.floor(canvas.width * 0.25);
    resetGame();

    if (!initialized) {
        attachInput();
        initialized = true;
    }
}

export function startGame() {
    if (!canvas || state.running || state.gameOver) {
        return;
    }
    state.running = true;
    state.lastTime = performance.now();
    rafId = requestAnimationFrame(loop);
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

    player.y = getGroundY() - player.height;
    player.vy = 0;
    player.onGround = true;

    obstacles = [];
    holes = [];
    enemies = [];
    projectiles = [];
    obstacleTimer = 1.4;
    enemyTimer = 2.5;
    shootCooldown = 0;

    notifyScore();
    notifyLives();
    render();
}

export function applyVoiceCommand(command) {
    if (!command) {
        return;
    }
    const normalized = command.toLowerCase();
    let executed = false;
    if (normalized === 'jump') {
        executed = tryJump(true);
    } else if (normalized === 'shoot') {
        executed = tryShoot(true);
    }
    if (executed) {
        addScore(50);
        emitEvent('voice-command', normalized);
    }
}

export function isVoiceSupported() {
    return !!(window.SpeechRecognition || window.webkitSpeechRecognition);
}

export function startVoiceControl(ref) {
    voice.dotNetRef = ref;
    if (!isVoiceSupported()) {
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
            if (command) {
                applyVoiceCommand(command);
                if (voice.dotNetRef) {
                    voice.dotNetRef.invokeMethodAsync('OnVoiceCommand', command);
                }
            }
        }
    };

    voice.recognition.onerror = (event) => {
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
                errorMsg = 'Network error during speech recognition.';
                break;
            default:
                errorMsg = `Speech recognition error: ${event.error}`;
        }
        if (voice.dotNetRef) {
            voice.dotNetRef.invokeMethodAsync('OnVoiceError', errorMsg);
        }
    };

    voice.recognition.onend = () => {
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
    window.addEventListener('keydown', (event) => {
        if (keysDown.has(event.code)) {
            return;
        }
        keysDown.add(event.code);
        if (event.code === 'Space' || event.code === 'ArrowUp') {
            event.preventDefault();
            tryJump(false);
        }
        if (event.code === 'KeyS') {
            event.preventDefault();
            tryShoot(false);
        }
    });

    window.addEventListener('keyup', (event) => {
        keysDown.delete(event.code);
    });
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

    player.vy += CONFIG.gravity * frame;
    player.y += player.vy * frame;

    const groundY = getGroundY();
    const overHole = isOverHole(player.x + player.width * 0.5);

    if (!overHole && player.y + player.height >= groundY) {
        player.y = groundY - player.height;
        player.vy = 0;
        player.onGround = true;
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
    });
    enemies = enemies.filter(enemy => enemy.x + enemy.width > -80);

    projectiles.forEach(projectile => {
        projectile.x += projectile.speed * frame;
    });
    projectiles = projectiles.filter(projectile => projectile.x < canvas.width + 120);

    checkCollisions();
    checkObstacleClears();
}

function render() {
    if (!ctx || !canvas) {
        return;
    }
    const groundY = getGroundY();
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = COLORS.sky;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = COLORS.ground;
    ctx.fillRect(0, groundY, canvas.width, CONFIG.groundHeight);

    holes.forEach(hole => {
        ctx.fillStyle = COLORS.sky;
        ctx.fillRect(hole.x, groundY, hole.width, CONFIG.groundHeight);
    });

    obstacles.forEach(obstacle => {
        ctx.fillStyle = COLORS.rock;
        ctx.fillRect(obstacle.x, obstacle.y, obstacle.width, obstacle.height);
    });

    enemies.forEach(enemy => {
        ctx.fillStyle = COLORS.enemy;
        ctx.fillRect(enemy.x, enemy.y, enemy.width, enemy.height);
    });

    projectiles.forEach(projectile => {
        ctx.fillStyle = COLORS.projectile;
        ctx.fillRect(projectile.x, projectile.y, projectile.width, projectile.height);
    });

    const isInvincible = isInvincibleNow();
    const blink = isInvincible && Math.floor(state.time * 10) % 2 === 0;
    if (!blink) {
        ctx.fillStyle = COLORS.player;
        ctx.fillRect(player.x, player.y, player.width, player.height);
    }
}

function tryJump(isVoice) {
    if (!state.running || state.gameOver || !player.onGround) {
        return false;
    }
    player.vy = CONFIG.jumpVelocity;
    player.onGround = false;
    if (isVoice) {
        emitEvent('voice-jump', 'jump');
    }
    return true;
}

function tryShoot(isVoice) {
    if (!state.running || state.gameOver || shootCooldown > 0) {
        return false;
    }
    shootCooldown = CONFIG.shootCooldown;
    projectiles.push({
        x: player.x + player.width + 4,
        y: player.y + player.height / 2 - 4,
        width: 12,
        height: 6,
        speed: state.scrollSpeed * 2.2 + 3
    });
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
        width: randomRange(70, 130)
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
            obstacle.hit = true;
            if (!invincible) {
                loseLife('hit');
            }
        }
    });

    enemies.forEach(enemy => {
        if (intersects(playerRect, enemy)) {
            if (!invincible) {
                loseLife('enemy');
            }
        }
    });

    for (let e = enemies.length - 1; e >= 0; e -= 1) {
        for (let p = projectiles.length - 1; p >= 0; p -= 1) {
            if (intersects(enemies[e], projectiles[p])) {
                enemies.splice(e, 1);
                projectiles.splice(p, 1);
                addScore(25);
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
            if (player.y + player.height < groundY - 4 && !obstacle.hit) {
                addScore(10);
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

    if (state.lives <= 0) {
        state.gameOver = true;
        state.running = false;
        emitEvent('game-over', reason);
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
