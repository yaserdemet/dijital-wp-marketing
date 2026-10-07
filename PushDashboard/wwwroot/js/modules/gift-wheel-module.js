/**
 * Pushonica Gift Wheel Module - Standalone Version
 * This script provides a complete gift wheel implementation
 * that loads configuration from R2 storage and communicates
 * with the Pushonica API.
 */

(function () {
    'use strict';
    class PushonicaGiftWheel {
        constructor(options = {}) {
            this.options = options;
            this.companyId = options.companyId || this.getCompanyId();
            this.apiBaseUrl = "https://pushadmin-production.up.railway.app";
            this.r2BaseUrl = "https://assets.pushonica.com";
            this.config = options.config || null;
            this.isInitialized = false;
            this.wheelContainer = null;
            this.canvas = null;
            this.ctx = null;
            this.isSpinning = false;
            this.currentRotation = 0;
            this.cookieKey = `pushonica_gift_wheel_${this.companyId}`;
            this.targetElement = options.targetElement || document.body;
            this.styleId = options.styleId || 'pushonica-wheel-styles';
            this.containerId = options.containerId || 'pushonica-wheel-modal';
            this.testMode = options.testMode || false;

            // Audio Setup
            this.audioContext = null;
            this.audioEnabled = false;

            if (this.companyId || this.config) {
                if (!options.manualInit) {
                    this.init();
                }
            }
        }

        initAudio() {
            // Check config override
            if (this.config && typeof this.config.soundEnabled !== 'undefined') {
                 this.audioEnabled = this.config.soundEnabled;
            }

            if (!this.audioEnabled || this.audioContext) return;
            try {
                const AudioContext = window.AudioContext || window.webkitAudioContext;
                if (AudioContext) {
                    this.audioContext = new AudioContext();
                }
            } catch (e) {
                console.warn('Web Audio API not supported');
                this.audioEnabled = false;
            }
        }

        playTickSound() {
            if (!this.audioEnabled || !this.audioContext) return;
            
            if (this.audioContext.state === 'suspended') {
                this.audioContext.resume();
            }

            try {
                const t = this.audioContext.currentTime;

                const oscLow = this.audioContext.createOscillator();
                const gainLow = this.audioContext.createGain();
                
                oscLow.connect(gainLow);
                gainLow.connect(this.audioContext.destination);

                oscLow.type = 'sine';
                oscLow.frequency.setValueAtTime(150, t);
                oscLow.frequency.exponentialRampToValueAtTime(50, t + 0.1);

                gainLow.gain.setValueAtTime(0, t);
                gainLow.gain.linearRampToValueAtTime(0.5, t + 0.005);
                gainLow.gain.exponentialRampToValueAtTime(0.001, t + 0.1);

                oscLow.start(t);
                oscLow.stop(t + 0.1);

                const oscMid = this.audioContext.createOscillator();
                const gainMid = this.audioContext.createGain();
                
                const filter = this.audioContext.createBiquadFilter();
                filter.type = 'lowpass';
                filter.frequency.value = 800;

                oscMid.connect(filter);
                filter.connect(gainMid);
                gainMid.connect(this.audioContext.destination);

                oscMid.type = 'triangle'; 
                oscMid.frequency.setValueAtTime(300, t);
                
                gainMid.gain.setValueAtTime(0, t);
                gainMid.gain.linearRampToValueAtTime(0.2, t + 0.005);
                gainMid.gain.exponentialRampToValueAtTime(0.001, t + 0.05);

                oscMid.start(t);
                oscMid.stop(t + 0.05);

            } catch (e) {
            }
        }

        playWinSound() {
            if (!this.audioEnabled || !this.audioContext) return;

            if (this.audioContext.state === 'suspended') {
                this.audioContext.resume();
            }

            try {
                const t = this.audioContext.currentTime;
                // C4, E4, G4, B4, C5
                const frequencies = [261.63, 329.63, 392.00, 493.88, 523.25];
                
                frequencies.forEach((freq, i) => {
                    const osc = this.audioContext.createOscillator();
                    const gain = this.audioContext.createGain();
                    
                    osc.connect(gain);
                    gain.connect(this.audioContext.destination);

                    osc.type = 'sine';
                    osc.frequency.value = freq;

                    const detune = (Math.random() * 4) - 2; 
                    osc.detune.value = detune;

                    const startTime = t;
                    const duration = 2.0; // Uzun, yankılanan bir ses

                    gain.gain.setValueAtTime(0, startTime);
                    gain.gain.linearRampToValueAtTime(0.15, startTime + 0.1); // Yumuşak giriş
                    gain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);

                    osc.start(startTime);
                    osc.stop(startTime + duration);
                });
                
                const subOsc = this.audioContext.createOscillator();
                const subGain = this.audioContext.createGain();
                subOsc.connect(subGain);
                subGain.connect(this.audioContext.destination);
                subOsc.type = 'sine';
                subOsc.frequency.value = 130.81; // C3
                
                subGain.gain.setValueAtTime(0, t);
                subGain.gain.linearRampToValueAtTime(0.2, t + 0.2);
                subGain.gain.exponentialRampToValueAtTime(0.001, t + 2.5);
                
                subOsc.start(t);
                subOsc.stop(t + 2.5);

            } catch (e) {
            }
        }

        async checkEligibility() {
            if (this.options.livePreview) return true;

            try {
                const response = await fetch(`${this.apiBaseUrl}/api/gift-wheel/${this.companyId}/eligibility`);
                if (response.ok) {
                    const data = await response.json();
                    return data.canSpin !== false;
                }
                return true; 
            } catch (error) {
                return true;
            }
        }

        async init() {
            if (this.isInitialized) return;

            try {
                if (!this.config) {
                    await this.loadConfigFromR2();
                }

                if (!this.config) return;

                // HANDLE WHEEL SIZE DEFAULT
                if (!this.config.wheelSize) {
                    const isMobile = window.innerWidth < 768;
                    this.config.wheelSize = isMobile ? 230 : 300;
                }

                const isEligible = await this.checkEligibility();
                if (!isEligible) {
                    return;
                }

                this.renderWheel();
                this.checkAndAutoOpen();
                this.isInitialized = true;
            } catch (error) {
                console.error('[GiftWheel] Initialization failed:', error);
            }
        }

        updateConfig(config) {
            this.config = { ...this.config, ...config };
            
            // HANDLE WHEEL SIZE DEFAULT RECALCULATION
            if (!this.config.wheelSize) {
                const isMobile = window.innerWidth < 768;
                this.config.wheelSize = isMobile ? 230 : 300;
            }

            this.injectStyles();
            
            if (this.options.livePreview) {
                this.renderWheel();
            }
        }

        checkAndAutoOpen() {
            if (this.options.livePreview || !this.config) return;

            // Session storage kontrolü - bu oturumda daha önce açıldı mı?
            const sessionKey = `pushonica_gift_wheel_shown_${this.companyId}`;
            if (sessionStorage.getItem(sessionKey)) return;

            // Otomatik aç ve işaretle
            setTimeout(() => {
                this.showWheelModal();
                sessionStorage.setItem(sessionKey, 'true');
            }, 1000);
        }

        async loadConfigFromR2() {
            try {
                const configUrl = `${this.r2BaseUrl}/${this.companyId}/giftWheel.json`;
                const response = await fetch(configUrl);

                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}`);
                }

                this.config = await response.json();
            } catch (error) {
                console.warn('[GiftWheel] Failed to load config from R2:', error.message);

                try {
                    const apiResponse = await fetch(`${this.apiBaseUrl}/api/gift-wheel/${this.companyId}/config`);
                    if (apiResponse.ok) {
                        this.config = await apiResponse.json();
                    } else {
                        throw new Error('API fallback failed');
                    }
                } catch (apiError) {
                    console.error('[GiftWheel] Both R2 and API config loading failed:', apiError.message);
                    return;
                }
            }
        }

        getCompanyId() {
            const scripts = document.querySelectorAll('script[data-module-id]');
            for (let script of scripts) {
                const id = script.getAttribute('data-module-id');
                if (id) return id;
            }

            return null;
        }

        renderWheel() {
            if (this.options.livePreview) {
                this.renderInlineWheel();
            } else {
                this.createFloatingButton();
            }
        }

        renderInlineWheel() {
            this.injectStyles();
            
            // Clear container
            this.targetElement.innerHTML = '';
            
            const content = `
                <div class="pushonica-modal-inline" style="display: flex; justify-content: center; align-items: center; width: 100%; height: 100%;">
                    <div class="pushonica-wheel-section" style="width: 100%; display: flex; justify-content: center;">
                        <div class="pushonica-wheel-container" style="width: 100%; max-width: ${this.config.wheelSize || 300}px; aspect-ratio: 1; margin: 0; padding: 0; position: relative;">
                            <canvas class="pushonica-wheel-canvas" width="${this.config.wheelSize || 300}" height="${this.config.wheelSize || 300}" style="width: 100%; height: 100%; box-shadow: none !important;"></canvas>
                        </div>
                    </div>
                </div>
            `;
            
            this.targetElement.innerHTML = content;
            
            const canvas = this.targetElement.querySelector('.pushonica-wheel-canvas');
            // Remove box-shadow from canvas via JS to be sure, though style attribute handles it
            canvas.style.boxShadow = 'none';

            this.setupWheel(canvas);
        }

        createFloatingButton() {
            const existingButton = document.getElementById('pushonica-gift-wheel-btn');
            if (existingButton) existingButton.remove();

            const button = document.createElement('div');
            button.id = 'pushonica-gift-wheel-btn';
            
            const iconSvg = `
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="pushonica-gift-icon-svg">
                <polyline points="20 12 20 22 4 22 4 12"></polyline>
                <rect x="2" y="7" width="20" height="5"></rect>
                <line x1="12" y1="22" x2="12" y2="7"></line>
                <path d="M12 7H7.5a2.5 2.5 0 0 1 0-5C11 2 12 7 12 7z"></path>
                <path d="M12 7h4.5a2.5 2.5 0 0 0 0-5C13 2 12 7 12 7z"></path>
            </svg>`;

            button.innerHTML = `
                <div class="pushonica-wheel-launcher-particles">
                    <span class="pushonica-particle pushonica-p1">✨</span>
                    <span class="pushonica-particle pushonica-p2">✨</span>
                    <span class="pushonica-particle pushonica-p3">✨</span>
                </div>
                <div class="pushonica-wheel-launcher-glow"></div>
                <div class="pushonica-wheel-icon-wrapper">
                    ${iconSvg}
                </div>
                <span class="pushonica-wheel-text">${this.config?.buttonText || 'Şanslı Çark'}</span>
            `;

            this.injectStyles();
            button.addEventListener('click', () => this.showWheelModal());
            document.body.appendChild(button);

            setTimeout(() => button.classList.add('show'), 1000);
            
            setTimeout(() => button.classList.add('pushonica-pulse-attention'), 5000);
        }

        injectStyles() {
            if (document.getElementById(this.styleId)) {
                document.getElementById(this.styleId).remove();
            }

            const primaryColor = this.config?.primaryColor || '#6366f1';

            const buttonTextColor = this.getTextColorForBackground(primaryColor);
            const modalTextColor = this.config?.modalTextColor || '#1f2937';

            // Button position settings from config
            const buttonPosition = this.config?.buttonPosition || 'right';
            const buttonBottomPx = this.config?.buttonBottomPx ?? 20;
            const buttonMobileBottomPx = this.config?.buttonMobileBottomPx ?? 20;
            const buttonHorizontalPx = 24; // Horizontal margin (fixed)

            // Position CSS based on left/right setting
            const positionStyles = buttonPosition === 'left'
                ? `left: ${buttonHorizontalPx}px; right: auto;`
                : `right: ${buttonHorizontalPx}px; left: auto;`;

            // Primary color'dan rgba değeri oluştur (shadow için)
            const hex = primaryColor.replace('#', '');
            const r = parseInt(hex.substr(0, 2), 16);
            const g = parseInt(hex.substr(2, 2), 16);
            const b = parseInt(hex.substr(4, 2), 16);

            const styles = document.createElement('style');
            styles.id = this.styleId;
            styles.textContent = `
                #pushonica-gift-wheel-btn {
                    position: fixed;
                    bottom: ${buttonBottomPx}px;
                    ${positionStyles}
                    background: ${primaryColor};
                    background: linear-gradient(135deg, ${primaryColor}, rgba(0,0,0,0.2));
                    background-blend-mode: overlay;
                    color: ${buttonTextColor};
                    border: none;
                    border-radius: 40px;
                    padding: 8px 16px 8px 8px;
                    cursor: pointer;
                    box-shadow:
                        0 8px 20px -4px rgba(${r}, ${g}, ${b}, 0.5),
                        0 4px 10px -2px rgba(${r}, ${g}, ${b}, 0.3),
                        0 0 0 1px rgba(255,255,255,0.2) inset;
                    z-index: 999999;
                    display: flex;
                    align-items: center;
                    gap: 12px;
                    font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    opacity: 0;
                    transform: translateY(20px) scale(0.95);
                    transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
                    overflow: visible;
                }

                #pushonica-gift-wheel-btn::before {
                    content: '';
                    position: absolute;
                    inset: 0;
                    border-radius: inherit;
                    background: linear-gradient(to bottom, rgba(255,255,255,0.15), transparent);
                    pointer-events: none;
                }

                #pushonica-gift-wheel-btn::after {
                    content: '';
                    position: absolute;
                    top: 0;
                    left: -150%;
                    width: 100%;
                    height: 100%;
                    background: linear-gradient(
                        90deg,
                        transparent,
                        rgba(255, 255, 255, 0.25),
                        transparent
                    );
                    transform: skewX(-20deg);
                    animation: pushonica-shine-sweep 6s infinite 3s;
                    pointer-events: none;
                }

                @keyframes pushonica-shine-sweep {
                    0% { left: -150%; }
                    20%, 100% { left: 150%; }
                }

                #pushonica-gift-wheel-btn.show {
                    opacity: 1;
                    transform: translateY(0) scale(1);
                }

                #pushonica-gift-wheel-btn:hover {
                    transform: translateY(-4px) scale(1.02);
                    box-shadow: 
                        0 15px 30px -5px rgba(${r}, ${g}, ${b}, 0.6),
                        0 8px 15px -4px rgba(${r}, ${g}, ${b}, 0.4),
                        0 0 0 1px rgba(255,255,255,0.3) inset;
                }
                
                #pushonica-gift-wheel-btn:active {
                    transform: translateY(1px) scale(0.98);
                }

                #pushonica-gift-wheel-btn.pushonica-pulse-attention {
                    animation: pushonica-pulse-attention 3s infinite;
                }

                .pushonica-wheel-launcher-particles {
                    position: absolute;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    pointer-events: none;
                    z-index: -1;
                }

                .pushonica-particle {
                    position: absolute;
                    font-size: 10px;
                    opacity: 0;
                }

                .pushonica-particle.pushonica-p1 { top: -5px; right: 10%; animation: pushonica-particle-float 3s ease-in-out infinite 0s; }
                .pushonica-particle.pushonica-p2 { bottom: -5px; left: 20%; animation: pushonica-particle-float 3s ease-in-out infinite 1s; }
                .pushonica-particle.pushonica-p3 { top: 50%; right: -8px; animation: pushonica-particle-float 3s ease-in-out infinite 0.5s; }

                @keyframes pushonica-particle-float {
                    0%, 100% { transform: translate(0, 0) scale(0); opacity: 0; }
                    50% { transform: translate(0, -5px) scale(1.2); opacity: 1; text-shadow: 0 0 5px gold; }
                }

                .pushonica-wheel-icon-wrapper {
                    background: rgba(255, 255, 255, 0.2);
                    backdrop-filter: blur(4px);
                    width: 40px;
                    height: 40px;
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    position: relative;
                    z-index: 10;
                    box-shadow: 
                        inset 0 0 0 1px rgba(255,255,255,0.2),
                        0 2px 4px rgba(0,0,0,0.1);
                }

                .pushonica-gift-icon-svg {
                    width: 22px;
                    height: 22px;
                    filter: drop-shadow(0 2px 3px rgba(0,0,0,0.2));
                    animation: pushonica-gift-shake 5s ease-in-out infinite;
                }

                .pushonica-wheel-text {
                    font-weight: 700;
                    font-size: 15px;
                    letter-spacing: -0.01em;
                    white-space: nowrap;
                    padding-right: 8px;
                    padding-left: 2px;
                    text-shadow: 0 1px 2px rgba(0,0,0,0.1);
                }

                .pushonica-wheel-launcher-glow {
                    position: absolute;
                    top: -10px;
                    left: -10px;
                    right: -10px;
                    bottom: -10px;
                    border-radius: 60px;
                    background: radial-gradient(circle at center, rgba(${r}, ${g}, ${b}, 0.2), transparent 70%);
                    opacity: 0;
                    transition: opacity 0.3s;
                    pointer-events: none;
                    z-index: -1;
                }
                
                #pushonica-gift-wheel-btn:hover .pushonica-wheel-launcher-glow {
                    opacity: 1;
                }

                @keyframes pushonica-gift-shake {
                    0%, 100% { transform: rotate(0deg) scale(1); }
                    80% { transform: rotate(0deg) scale(1); }
                    85% { transform: rotate(-10deg) scale(1.1); }
                    90% { transform: rotate(10deg) scale(1.1); }
                    95% { transform: rotate(-5deg) scale(1.05); }
                }

                @keyframes pushonica-pulse-attention {
                    0% { box-shadow: 0 0 0 0 rgba(${r}, ${g}, ${b}, 0.7); }
                    50% { box-shadow: 0 0 0 15px rgba(${r}, ${g}, ${b}, 0); }
                    100% { box-shadow: 0 0 0 0 rgba(${r}, ${g}, ${b}, 0); }
                }

                /* Mobile Optimization */
                @media (max-width: 768px) {
                    #pushonica-gift-wheel-btn {
                        padding: 0;
                        width: 56px;
                        height: 56px;
                        border-radius: 50%;
                        justify-content: center;
                        ${positionStyles}
                        bottom: ${buttonMobileBottomPx}px;
                    }

                    .pushonica-wheel-text {
                        display: none;
                    }

                    .pushonica-wheel-icon-wrapper {
                        background: transparent;
                        backdrop-filter: none;
                        box-shadow: none;
                        width: 100%;
                        height: 100%;
                    }

                    .pushonica-gift-icon-svg {
                        width: 26px;
                        height: 26px;
                    }

                    .pushonica-particle { display: none; }
                }

                .pushonica-modal-overlay {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: 
                        radial-gradient(circle at 20% 80%, rgba(120, 119, 198, 0.3) 0%, transparent 50%),
                        radial-gradient(circle at 80% 20%, rgba(255, 119, 198, 0.3) 0%, transparent 50%),
                        radial-gradient(circle at 40% 40%, rgba(120, 219, 255, 0.2) 0%, transparent 50%),
                        rgba(0, 0, 0, 0.85);
                    z-index: 1000000;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    opacity: 0;
                    transition: opacity 0.3s ease;
                    padding: 20px;
                    box-sizing: border-box;
                    overflow-y: auto;
                    animation: pushonica-modal-bg-float 20s ease-in-out infinite;
                }

                .pushonica-modal-overlay.show {
                    opacity: 1;
                }

                .pushonica-modal {
                    background: white;
                    border-radius: 18px;
                    max-width: 95vw;
                    max-height: 95vh;
                    height: auto;
                    width: fit-content;
                    min-width: 250px;
                    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.15);
                    transform: scale(0.8) translateY(20px);
                    transition: transform 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
                    position: relative;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    overflow: hidden;
                    display: flex;
                    flex-direction: column;
                    text-align: center;
                }

                .pushonica-modal-scrollbox {
                    overflow-y: auto;
                    padding: 16px;
                    padding-bottom: 40px;
                    flex: 1;
                    width: 100%;
                    min-height: 0;
                    box-sizing: border-box;
                    position: relative;
                    z-index: 1;
                }

                .pushonica-powered-by {
                    position: absolute;
                    bottom: 12px;
                    right: 16px;
                    z-index: 30;
                    opacity: 0.9;
                    transition: all 0.3s ease;
                    display: block;
                    line-height: 0;
                    padding: 4px;
                    border-radius: 4px;
                }
                
                .pushonica-powered-by:hover {
                    opacity: 1;
                }

                .pushonica-powered-by img {
                    height: 18px; 
                    width: auto;
                    display: block;
                }

                .pushonica-modal-overlay.show .pushonica-modal {
                    transform: scale(1) translateY(0);
                }

                .pushonica-close-btn {
                    position: absolute;
                    top: 8px;
                    right: 8px;
                    background: none;
                    border: none;
                    font-size: 24px;
                    cursor: pointer;
                    z-index: 20;
                    color: ${modalTextColor};
                    opacity: 0.5;
                    width: 20px;
                    height: 20px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    border-radius: 50%;
                    transition: all 0.2s ease;
                }

                .pushonica-close-btn:hover {
                    background: rgba(0,0,0,0.05);
                    color: ${modalTextColor};
                    opacity: 1;
                }

                .pushonica-wheel-title {
                    font-size: 16px;
                    font-weight: 700;
                    color: ${modalTextColor};
                    margin-bottom: 4px;
                    margin-top: 8px;
                    line-height: 1.2;
                }

                .pushonica-wheel-subtitle {
                    font-size: 11px;
                    color: ${modalTextColor};
                    opacity: 0.7;
                    margin-bottom: 12px;
                    line-height: 1.4;
                }

                .pushonica-wheel-container {
                    position: relative;
                    display: inline-block;
                    margin-bottom: 20px;
                }

                .pushonica-wheel-canvas {
                    border-radius: 50%;
                    box-shadow: 0 15px 35px rgba(0, 0, 0, 0.3), 
                                0 5px 15px rgba(0, 0, 0, 0.2),
                                inset 0 1px 0 rgba(255, 255, 255, 0.3);
                    max-width: 100%;
                    height: auto;
                    position: relative;
                }

                @keyframes pushonica-pulse-glow {
                    0% { 
                        opacity: 0.3;
                        transform: scale(1);
                    }
                    100% { 
                        opacity: 0.6;
                        transform: scale(1.05);
                    }
                }

                @keyframes pushonica-logo-pulse {
                    0%, 100% { transform: translate(-50%, -50%) scale(1); }
                    50% { transform: translate(-50%, -50%) scale(1.08); }
                }

                @keyframes pushonica-modal-bg-float {
                    0%, 100% {
                        background: 
                            radial-gradient(circle at 20% 80%, rgba(120, 119, 198, 0.25) 0%, transparent 40%),
                            radial-gradient(circle at 80% 20%, rgba(255, 119, 198, 0.25) 0%, transparent 40%),
                            radial-gradient(circle at 40% 40%, rgba(120, 219, 255, 0.15) 0%, transparent 40%),
                            rgba(0, 0, 0, 0.85);
                    }
                    50% {
                        background: 
                            radial-gradient(circle at 60% 30%, rgba(120, 119, 198, 0.25) 0%, transparent 40%),
                            radial-gradient(circle at 40% 70%, rgba(255, 119, 198, 0.25) 0%, transparent 40%),
                            radial-gradient(circle at 80% 60%, rgba(120, 219, 255, 0.15) 0%, transparent 40%),
                            rgba(0, 0, 0, 0.85);
                    }
                }

                .pushonica-spin-button {
                    background: ${primaryColor};
                    color: ${buttonTextColor};
                    border: none;
                    border-radius: 10px;
                    padding: 10px 20px;
                    font-size: 14px;
                    font-weight: 600;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    box-shadow: 0 3px 12px rgba(${r}, ${g}, ${b}, 0.3);
                    min-width: 100px;
                    width: 100%;
                    font-family: 'Poppins', sans-serif; 
                }

                .pushonica-spin-button:hover:not(:disabled) {
                    transform: translateY(-2px);
                    box-shadow: 0 6px 20px rgba(${r}, ${g}, ${b}, 0.4);
                }

                .pushonica-spin-button:disabled {
                    opacity: 0.6;
                    cursor: not-allowed;
                }

                .pushonica-form-container {
                    text-align: left;
                }

                .pushonica-form-group {
                    margin-bottom: 10px;
                }

                .pushonica-form-label {
                    display: block;
                    margin-bottom: 2px;
                    font-size: 10px;
                    font-weight: 600;
                    color: ${modalTextColor};
                    opacity: 0.85;
                    margin-left: 4px;
                }

                .pushonica-form-input {
                    width: 100%;
                    padding: 8px 12px;
                    border: 1px solid #e5e7eb;
                    border-radius: 12px;
                    font-size: 12px;
                    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                    box-sizing: border-box;
                    background-color: #f9fafb;
                    color: #1f2937;
                    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
                }

                .pushonica-form-input:hover {
                    background-color: #ffffff;
                    border-color: #d1d5db;
                }

                .pushonica-form-input:focus {
                    outline: none;
                    border-color: ${primaryColor};
                    background-color: #ffffff;
                    box-shadow: 0 0 0 4px rgba(${r}, ${g}, ${b}, 0.1);
                }
                
                .pushonica-form-input::placeholder {
                    color: #9ca3af;
                }

                .pushonica-form-input.pushonica-input-error {
                    border-color: #ef4444;
                    background-color: #fef2f2;
                }

                .pushonica-validation-error {
                    color: #ef4444;
                    font-size: 10px;
                    margin-top: 4px;
                    margin-left: 4px;
                    display: none;
                    font-weight: 500;
                    animation: pushonica-slideDown 0.2s ease-out forwards;
                }

                @keyframes pushonica-slideDown {
                    from { opacity: 0; transform: translateY(-5px); }
                    to { opacity: 1; transform: translateY(0); }
                }

                .pushonica-consent-group {
                    margin-top: 10px;
                    margin-bottom: 10px;
                }

                .pushonica-checkbox-label {
                    display: flex;
                    align-items: flex-start;
                    cursor: pointer;
                    font-size: 10px;
                    color: ${modalTextColor};
                    opacity: 0.7;
                    line-height: 1.4;
                }

                .pushonica-form-checkbox {
                    appearance: none;
                    width: 16px;
                    height: 16px;
                    border: 2px solid #d1d5db;
                    border-radius: 3px;
                    background-color: #ffffff;
                    cursor: pointer;
                    position: relative;
                    flex-shrink: 0;
                    margin-top: 1px;
                    transition: all 0.2s ease;
                }

                .pushonica-form-checkbox:checked {
                    background-color: #6366f1;
                    border-color: #6366f1;
                }

                .pushonica-form-checkbox:checked::after {
                    content: '✓';
                    position: absolute;
                    top: 50%;
                    left: 50%;
                    transform: translate(-50%, -50%);
                    color: white;
                    font-size: 11px;
                    font-weight: bold;
                }

                .pushonica-form-checkbox:focus {
                    outline: none;
                    box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
                }

                .pushonica-result-message {
                    margin: 16px 0;
                    padding: 12px;
                    border-radius: 10px;
                    font-weight: 500;
                    text-align: center;
                    line-height: 1.4;
                    font-size: 14px;
                }

                .pushonica-result-message.pushonica-win {
                    background: linear-gradient(135deg, #10b981, #059669);
                    color: white;
                }

                .pushonica-result-message.pushonica-message {
                    background: linear-gradient(135deg, #10b981, #059669);
                    color: white;
                }

                .pushonica-result-message.pushonica-retry {
                    background: #f3f4f6;
                    color: #6b7280;
                    border: 2px solid #d1d5db;
                }

                .pushonica-result-message.pushonica-lose {
                    background: #fef2f2;
                    color: #dc2626;
                    border: 1px solid #fecaca;
                }

                /* Voucher specific styles */
                .pushonica-voucher-display {
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    gap: 8px;
                    width: 100%;
                }
                
                .pushonica-voucher-label {
                    font-size: 13px;
                    font-weight: 600;
                    opacity: 0.9;
                    text-transform: uppercase;
                    letter-spacing: 0.5px;
                    color: rgba(255, 255, 255, 0.95);
                }

                .pushonica-voucher-box {
                    background: rgba(255, 255, 255, 0.25);
                    border: 2px dashed rgba(255, 255, 255, 0.6);
                    border-radius: 12px;
                    padding: 12px 16px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: 12px;
                    width: auto;
                    min-width: 200px;
                    margin: 8px 0;
                    cursor: pointer;
                    transition: all 0.2s;
                    position: relative;
                }
                
                .pushonica-voucher-box:hover {
                    background: rgba(255, 255, 255, 0.35);
                    border-color: rgba(255, 255, 255, 0.8);
                }

                .pushonica-voucher-text {
                    font-family: monospace;
                    font-size: 24px;
                    font-weight: 800;
                    letter-spacing: 1px;
                    color: #ffffff;
                    text-shadow: 0 1px 2px rgba(0,0,0,0.1);
                    user-select: all;
                }

                .pushonica-copy-btn {
                    background: white;
                    color: #10b981; /* Fallback green */
                    border: none;
                    width: 36px;
                    height: 36px;
                    border-radius: 8px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    cursor: pointer;
                    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
                    transition: all 0.2s;
                    flex-shrink: 0;
                }
                
                /* If primary color is dark, use it for text on white button, else keep green/default */
                /* Dynamic style injection handles this usually, but here we hardcode for safety over green bg */
                .pushonica-copy-btn {
                    color: ${primaryColor};
                }

                .pushonica-copy-btn:hover {
                    box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.15);
                    transform: translateY(-2px);
                    filter: brightness(1.05);
                }
                
                .pushonica-copy-btn:active {
                    transform: translateY(0);
                    filter: brightness(0.95);
                }

                .pushonica-voucher-info-text {
                    font-size: 11px;
                    opacity: 0.8;
                    margin-top: 8px;
                }
                
                .pushonica-voucher-discount {
                    font-size: 16px; 
                    font-weight: bold;
                    margin-top: 4px;
                }
                
                .pushonica-voucher-validity {
                    font-size: 11px; 
                    opacity: 0.9;
                }

                .pushonica-copy-feedback {
                   position: absolute;
                   bottom: -28px;
                   left: 50%;
                   transform: translateX(-50%);
                   background: #1f2937;
                   color: #ffffff;
                   padding: 4px 10px;
                   border-radius: 6px;
                   font-size: 11px;
                   font-weight: 500;
                   white-space: nowrap;
                   pointer-events: none;
                   z-index: 20;
                   opacity: 0;
                   box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);
                }
                
                .pushonica-copy-feedback::before {
                    content: '';
                    position: absolute;
                    top: -4px;
                    left: 50%;
                    transform: translateX(-50%);
                    border-left: 4px solid transparent;
                    border-right: 4px solid transparent;
                    border-bottom: 4px solid #1f2937;
                }
                
                @keyframes pushonica-tooltip-pop {
                    0% { opacity: 0; transform: translateX(-50%) scale(0.9); }
                    100% { opacity: 1; transform: translateX(-50%) scale(1); }
                }
                
                @keyframes pushonica-tooltip-fade {
                    0% { opacity: 1; transform: translateX(-50%) scale(1); }
                    100% { opacity: 0; transform: translateX(-50%) scale(0.95); }
                }

                .pushonica-confetti {
                    position: fixed;
                    width: 10px;
                    height: 10px;
                    background: #6366f1;
                    animation: pushonica-confetti-fall 3s linear forwards;
                    z-index: 2000000;
                    pointer-events: none;
                    border-radius: 2px;
                }

                .pushonica-confetti-emoji {
                    position: fixed;
                    font-size: 20px;
                    animation: pushonica-confetti-fall 3s linear forwards;
                    z-index: 2000000;
                    pointer-events: none;
                    user-select: none;
                }

                .pushonica-modal-content-wrapper {
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    width: 100%;
                    gap: 20px;
                }

                .pushonica-modal-content-wrapper.pushonica-row {
                    flex-direction: row;
                    align-items: center;
                    justify-content: center;
                    gap: 32px;
                    padding: 0 10px;
                }

                .pushonica-wheel-section {
                    flex: 0 0 auto;
                }

                .pushonica-form-section {
                    flex: 1;
                    width: 100%;
                    min-width: 250px;
                    display: flex;
                    flex-direction: column;
                }

                .pushonica-modal.pushonica-layout-row {
                    max-width: 95vw;
                    width: auto;
                }

                @keyframes pushonica-confetti-fall {
                    0% {
                        transform: translateY(0) rotate(0deg) scale(1);
                        opacity: 1;
                    }
                    25% {
                        transform: translateY(25vh) rotate(180deg) scale(1.1);
                        opacity: 1;
                    }
                    50% {
                        transform: translateY(50vh) rotate(360deg) scale(1);
                        opacity: 0.9;
                    }
                    75% {
                        transform: translateY(75vh) rotate(540deg) scale(0.9);
                        opacity: 0.8;
                    }
                    100% {
                        transform: translateY(120vh) rotate(720deg) scale(0.6);
                        opacity: 0;
                    }
                }

                /* Mobile Responsive */
                @media (max-width: 768px) {
                    .pushonica-modal-content-wrapper.pushonica-row {
                        flex-direction: column;
                        gap: 12px;
                    }

                    .pushonica-wheel-section {
                        width: 100%;
                        display: flex;
                        justify-content: center;
                        margin-top: 5px;
                    }
                    
                    .pushonica-wheel-container {
                        margin-bottom: 10px;
                    }

                    .pushonica-modal.pushonica-layout-row {
                        max-width: 95vw;
                    }
                    
                    .pushonica-modal-scrollbox {
                        padding: 16px;
                    }
                    
                    .pushonica-wheel-icon {
                        font-size: 16px;
                    }
                    
                    .pushonica-wheel-text {
                        display: none;
                    }
                    
                    .pushonica-modal {
                        padding: 0;
                        border-radius: 12px;
                        width: auto;
                        max-width: 90vw;
                    }
                    
                    .pushonica-wheel-title {
                        font-size: 14px;
                        margin-top: 0;
                    }
                    
                    .pushonica-wheel-subtitle {
                        font-size: 10px;
                        margin-bottom: 8px;
                    }
                    
                    .pushonica-form-group {
                        margin-bottom: 6px;
                    }
                    
                    .pushonica-form-input {
                        padding: 8px 12px;
                        font-size: 10px;
                    }
                    
                    .pushonica-spin-button {
                        padding: 10px 20px;
                        font-size: 13px;
                        margin-top: 5px;
                    }
                    
                    .pushonica-close-btn {
                        top: 6px;
                        right: 6px;
                        width: 20px;
                        height: 20px;
                        font-size: 14px;
                        background: rgba(0,0,0,0.05);
                    }
                    
                    .pushonica-powered-by {
                        position: absolute;
                        bottom: 10px;
                        right: 10px;
                        margin: 0;
                        opacity: 0.8;
                    }

                    .pushonica-modal-scrollbox {
                        padding-bottom: 40px !important;
                    }
                }

                @media (max-width: 480px) {
                    .pushonica-modal-overlay {
                        padding: 20px;
                        align-items: center; /* Center vertical */
                    }
                    
                    .pushonica-modal {
                        width: auto;
                        max-width: 95%;
                        border-radius: 16px;
                        margin: 0 auto;
                        /* Ensure it doesn't look like bottom sheet */
                    }
                    
                    .pushonica-wheel-pointer {
                        border-left-width: 12px;
                        border-right-width: 12px;
                        border-top-width: 20px;
                    }
                }
            `;
            document.head.appendChild(styles);
        }

        getCookieData() {
            try {
                const cookieValue = document.cookie
                    .split('; ')
                    .find(row => row.startsWith(this.cookieKey))
                    ?.split('=')[1];

                return cookieValue ? JSON.parse(decodeURIComponent(cookieValue)) : null;
            } catch (error) {
                return null;
            }
        }

        setCookie(data) {
            const expires = new Date();
            expires.setTime(expires.getTime() + (365 * 24 * 60 * 60 * 1000)); // 1 year
            const cookieValue = encodeURIComponent(JSON.stringify(data));
            document.cookie = `${this.cookieKey}=${cookieValue}; expires=${expires.toUTCString()}; path=/`;
        }

        showWheelModal() {
            const existingModal = document.getElementById('pushonica-wheel-modal');
            if (existingModal) existingModal.remove();

            // Body scroll'unu devre dışı bırak
            document.body.style.overflow = 'hidden';

            const modal = document.createElement('div');
            modal.id = 'pushonica-wheel-modal';
            modal.className = 'pushonica-modal-overlay';

            const layout = this.config.layout || 'row';
            const isRow = layout === 'row';

            const titleHtml = `
                <div class="pushonica-wheel-title">${this.config.title}</div>
                <div class="pushonica-wheel-subtitle">${this.config.subtitle}</div>
            `;

            // RECALCULATE WHEEL SIZE FOR MODAL
            let wheelSize = this.config.wheelSize;
            if (!wheelSize) {
                 const isMobile = window.innerWidth < 768; // Or use modal check logic
                 wheelSize = isMobile ? 230 : 300;
                 console.log('[GiftWheel] Auto-size logic active:', { isMobile, wheelSize });
            } else {
                 console.log('[GiftWheel] Using configured size:', wheelSize);
            }

            modal.innerHTML = `
                <div class="pushonica-modal ${isRow ? 'pushonica-layout-row' : ''}" ${this.config.backgroundImageUrl ? `style="background-image: url('${this.config.backgroundImageUrl}'); background-size: cover; background-position: center; background-repeat: no-repeat;"` : ''}>
                    <button class="pushonica-close-btn"><svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg></button>
                    <div class="pushonica-modal-scrollbox">
                    ${!isRow ? titleHtml : ''}
                    
                    <div class="pushonica-modal-content-wrapper ${isRow ? 'pushonica-row' : ''}">
                        <div class="pushonica-wheel-section">
                            <div class="pushonica-wheel-container" style="width: 100%; max-width: ${wheelSize}px; aspect-ratio: 1; margin: 0 auto; position: relative;">
                                <canvas class="pushonica-wheel-canvas" width="${wheelSize}" height="${wheelSize}" style="width: 100%; height: 100%; object-fit: contain;"></canvas>
                            </div>
                        </div>
                        
                        <div class="pushonica-form-section" style="max-width: ${Math.max(wheelSize, 250)}px;">
                            ${isRow ? titleHtml : ''}
                            <div class="pushonica-form-container" id="user-form">
                                <div class="pushonica-form-group">
                                    <label class="pushonica-form-label">Ad Soyad</label>
                                    <input type="text" class="pushonica-form-input" id="name-input" placeholder="Ad Soyadınız">
                                </div>
                                ${this.config.requirePhone ? `
                                <div class="pushonica-form-group">
                                    <label class="pushonica-form-label">Telefon Numarası</label>
                                    <input type="tel" class="pushonica-form-input" id="phone-input" placeholder="+90 5XX XXX XX XX">
                                </div>
                                ` : ''}
                                ${this.config.requireEmail ? `
                                <div class="pushonica-form-group">
                                    <label class="pushonica-form-label">E-posta Adresi</label>
                                    <input type="email" class="pushonica-form-input" id="email-input" placeholder="ornek@email.com">
                                </div>
                                ` : ''}
                                ${(this.config.requirePhone || this.config.requireEmail) ? `
                                <div class="pushonica-form-group pushonica-consent-group">
                                    <label class="pushonica-checkbox-label" style="margin-bottom: 5px;">
                                        <input type="checkbox" class="pushonica-form-checkbox" id="consent-checkbox-1" required>
                                        <span style="margin-left: 8px;">Kampanya ve fırsatlardan haberdar olmak istiyorum. (<a href="${this.config.privacyPolicyUrl || '#'}" target="_blank" style="color: ${this.config?.primaryColor || '#6366f1'}; font-weight: 600; text-decoration: none;">Ticari İleti Aydınlatma Metni</a>'ni okudum.)</span>
                                    </label>
                                    <label class="pushonica-checkbox-label">
                                        <input type="checkbox" class="pushonica-form-checkbox" id="consent-checkbox-2" required>
                                        <span style="margin-left: 8px;"><a href="${this.config.termsUrl || '#'}" target="_blank" style="color: ${this.config?.primaryColor || '#6366f1'}; font-weight: 600; text-decoration: none;">KVKK kapsamında verilerimin işlenmesini</a>; SMS ve WhatsApp ile bana ulaşılmasını onaylıyorum.</span>
                                    </label>
                                </div>
                                ` : ''}
                            </div>
                            
                            <button class="pushonica-spin-button" id="spin-btn">${this.config.buttonText}</button>
                            
                            <div class="pushonica-result-message" id="result-message" style="display: none;"></div>
                        </div>
                    </div>
                    </div>
                    <a href="https://dijitaldesin.com" target="_blank" class="pushonica-powered-by" title="Powered by Dijitaldesin">
                        <img src="https://dijitaldesin.com/wp-content/uploads/2023/02/dijitaldesin.svg" alt="Dijitaldesin">
                    </a>
                </div>
            `;

            document.body.appendChild(modal);
            setTimeout(() => modal.classList.add('show'), 10);

            this.initAudio();

            // Setup wheel
            this.setupWheel(modal.querySelector('.pushonica-wheel-canvas'));

            // Setup spin button
            modal.querySelector('#spin-btn').addEventListener('click', () => this.handleSpin());

            // Setup form validation
            this.setupFormValidation(modal);

            // Setup close button properly
            const closeBtn = modal.querySelector('.pushonica-close-btn');
            closeBtn.addEventListener('click', () => this.closeModal());

            // Close modal when clicking outside
            modal.addEventListener('click', (e) => {
                if (e.target === modal) {
                    this.closeModal();
                }
            });
        }

        closeModal() {
            const modal = document.getElementById('pushonica-wheel-modal');
            if (modal) {
                modal.classList.remove('show');
                setTimeout(() => {
                    modal.remove();
                    // Body scroll'unu tekrar aktif et
                    document.body.style.overflow = '';
                }, 300);
            }
        }

        setupWheel(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d');

            // RECALCULATE SIZE - fallback to existing or auto
            let size = this.config.wheelSize;
            if (!size) {
                 const isMobile = window.innerWidth < 768;
                 size = isMobile ? 230 : 300;
            }
            
            const dpr = window.devicePixelRatio || 1;
            
            canvas.width = size * dpr;
            canvas.height = size * dpr;
            
            canvas.style.width = '100%';
            canvas.style.height = '100%';

            // Create HTML img overlay for logo instead of drawing on canvas (better quality)
            if (this.config.logoUrl) {
                this.logoLoaded = false;
                // Pre-load image to check validity
                const testImg = new Image();
                testImg.onload = () => {
                    this.logoLoaded = true;
                    this.createLogoOverlay(canvas);
                    this.drawWheel();
                    this.startIdleAnimation();
                };
                testImg.onerror = () => {
                    console.warn('[GiftWheel] Failed to load logo image:', this.config.logoUrl);
                    this.logoLoaded = false;
                    this.drawWheel();
                    this.startIdleAnimation();
                };
                testImg.src = this.config.logoUrl;
            } else {
                this.logoLoaded = false;
                this.drawWheel();
                this.startIdleAnimation();
            }
        }

        createLogoOverlay(canvas) {
            // Remove existing overlay if any
            const container = canvas.parentElement;
            const existing = container.querySelector('.pushonica-logo-overlay');
            if (existing) existing.remove();

            const logoPercent = (this.config.logoSizePercentage || 15) / 100;

            // Create wrapper div for logo (circle with white bg + shadow)
            const logoWrapper = document.createElement('div');
            logoWrapper.className = 'pushonica-logo-overlay';
            logoWrapper.style.cssText = `
                position: absolute;
                top: 50%; left: 50%;
                transform: translate(-50%, -50%);
                width: ${logoPercent * 100}%;
                height: ${logoPercent * 100}%;
                border-radius: 50%;
                background: #fff;
                box-shadow: 0 2px 10px rgba(0,0,0,0.2);
                display: flex;
                align-items: center;
                justify-content: center;
                overflow: hidden;
                z-index: 2;
                pointer-events: none;
            `;

            const img = document.createElement('img');
            img.src = this.config.logoUrl;
            img.alt = 'Logo';
            img.style.cssText = `
                max-width: 100%;
                max-height: 100%;
                object-fit: contain;
                border-radius: 50%;
            `;

            logoWrapper.appendChild(img);
            container.appendChild(logoWrapper);

            // Store reference for animation
            this.logoOverlayEl = logoWrapper;
        }

        drawWheel() {
            const ctx = this.ctx;
            const canvas = this.canvas;
            const centerX = canvas.width / 2;
            const centerY = canvas.height / 2;
            const canvasSize = Math.min(canvas.width, canvas.height);
            const scaleFactor = canvasSize / 300;
            const radius = Math.min(centerX, centerY) - (15 * scaleFactor);
            const prizes = this.config.prizes.filter(p => p.isActive);
            const segmentAngle = (2 * Math.PI) / prizes.length;

            // Calculate center logo radius for text positioning
            const logoPercent = (this.config.logoSizePercentage || 15) / 100;
            const logoCenterRadius = (canvasSize * logoPercent) / 2 + (4 * scaleFactor); // includes white bg padding
            // Fallback center circle radius when no logo
            const fallbackCenterRadius = 20 * scaleFactor;
            const hasLogo = this.logoLoaded && this.config.logoUrl;
            const effectiveCenterRadius = hasLogo ? logoCenterRadius : fallbackCenterRadius;

            // Clear canvas
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // Draw outer ring (decorative)
            ctx.beginPath();
            ctx.arc(centerX, centerY, radius + (10 * scaleFactor), 0, 2 * Math.PI);

            const primaryColor = this.config.primaryColor || '#ffd700';

            // Create gradient based on primary color
            const edgeGradient = ctx.createLinearGradient(centerX - radius, centerY - radius, centerX + radius, centerY + radius);
            edgeGradient.addColorStop(0, primaryColor);
            edgeGradient.addColorStop(0.5, this.shadeColor(primaryColor, 20)); // Lighter
            edgeGradient.addColorStop(1, primaryColor);

            ctx.fillStyle = edgeGradient;
            ctx.fill();

            ctx.strokeStyle = this.shadeColor(primaryColor, -20);
            ctx.lineWidth = 3 * scaleFactor;
            ctx.stroke();

            // Draw wheel segments
            prizes.forEach((prize, index) => {
                // Pointer üstte (0°) olduğu için, ilk segment pointer'ın altında başlamalı
                // Segment'leri -90° (pointer'ın altı) den başlatıyoruz
                const segmentOffset = -Math.PI / 2; // -90 derece offset
                const startAngle = (index * segmentAngle) + (this.currentRotation * Math.PI / 180) + segmentOffset;
                const endAngle = ((index + 1) * segmentAngle) + (this.currentRotation * Math.PI / 180) + segmentOffset;

                // Create gradient for each segment
                const gradient = ctx.createLinearGradient(
                    centerX - radius / 2, centerY - radius / 2,
                    centerX + radius / 2, centerY + radius / 2
                );

                const baseColor = prize.color || '#6366f1';
                gradient.addColorStop(0, this.shadeColor(baseColor, -10));
                gradient.addColorStop(0.5, baseColor);
                gradient.addColorStop(1, this.shadeColor(baseColor, 10));

                // Draw segment
                ctx.beginPath();
                ctx.moveTo(centerX, centerY);
                ctx.arc(centerX, centerY, radius, startAngle, endAngle);
                ctx.fillStyle = gradient;
                ctx.fill();

                // Add segment border
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 3 * scaleFactor;
                ctx.stroke();

                // Add subtle inner shadow effect
                ctx.beginPath();
                ctx.moveTo(centerX, centerY);
                ctx.arc(centerX, centerY, radius - (5 * scaleFactor), startAngle, endAngle);
                ctx.strokeStyle = 'rgba(0, 0, 0, 0.1)';
                ctx.lineWidth = 1 * scaleFactor;
                ctx.stroke();

                // Draw text - dynamically positioned based on center logo size
                const textAngle = startAngle + segmentAngle / 2;
                // Position text midway between the logo edge and the wheel edge
                const textRadius = effectiveCenterRadius + (radius - effectiveCenterRadius) * 0.5;

                ctx.save();

                // Adaptif yazı boyutu hesaplama - mevcut alana göre dinamik
                const availableSpace = radius - effectiveCenterRadius;
                const spaceRatio = availableSpace / radius; // 0-1 arası, logo büyüdükçe küçülür

                // Font boyutunu mevcut alana göre hesapla
                let baseFontSize = Math.round(canvasSize * 0.045 * Math.min(1, spaceRatio * 1.5));

                // Minimum değer
                baseFontSize = Math.max(7, baseFontSize);

                // Segment sayısına göre ince ayarlama
                const segmentCount = prizes.length;
                if (segmentCount > 8) {
                    baseFontSize = Math.round(baseFontSize * 0.85);
                } else if (segmentCount <= 4) {
                    baseFontSize = Math.round(baseFontSize * 1.15);
                }

                // Minimum değer kontrolü
                baseFontSize = Math.max(7, baseFontSize);

                const margin = 5 * scaleFactor;
                // Available space = from logo edge to wheel edge, text sits in the middle
                const availableRadial = radius - effectiveCenterRadius - margin * 2;
                const maxTextWidth = availableRadial * 0.9;

                const words = prize.name.split(' ');

                const textX = centerX + Math.cos(textAngle) * textRadius;
                const textY = centerY + Math.sin(textAngle) * textRadius;

                ctx.translate(textX, textY);

                // Yazıyı segmente paralel hizala (daha okunabilir)
                if (textAngle > Math.PI / 2 && textAngle < 3 * Math.PI / 2) {
                    ctx.rotate(textAngle + Math.PI); // Alt yarım için ters çevir
                } else {
                    ctx.rotate(textAngle);
                }

                // Text shadow için
                ctx.fillStyle = 'rgba(0, 0, 0, 0.4)';
                ctx.font = `bold ${baseFontSize}px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';

                // Akıllı satır bölme - mevcut alana göre dinamik karakter limiti
                const charsPerPixel = 0.12; // yaklaşık karakter/piksel oranı
                const maxLineLength = Math.max(6, Math.round(maxTextWidth * charsPerPixel / scaleFactor));
                let lines = [];

                if (prize.name.length > maxLineLength && prize.name.includes(' ')) {
                    // Uzun metinleri akıllıca böl
                    const wordsLine = prize.name.split(' ');
                    let currentLine = '';

                    wordsLine.forEach(word => {
                        if ((currentLine + ' ' + word).length <= maxLineLength) {
                            currentLine = currentLine ? currentLine + ' ' + word : word;
                        } else {
                            if (currentLine) lines.push(currentLine);
                            currentLine = word;
                        }
                    });
                    if (currentLine) lines.push(currentLine);
                } else {
                    // Kısa metinler tek satırda
                    lines = [prize.name];
                }

                // Çok satır varsa (3+) zorla 2 satıra indir
                if (lines.length > 2) {
                    const firstPart = lines.slice(0, Math.ceil(lines.length / 2)).join(' ');
                    const secondPart = lines.slice(Math.ceil(lines.length / 2)).join(' ');
                    lines = [firstPart, secondPart];
                }

                if (lines.length > 1) {
                    // Çok satırlı metin
                    const lineHeight = baseFontSize + (3 * scaleFactor);
                    const totalHeight = (lines.length - 1) * lineHeight;
                    const startY = -totalHeight / 2;

                    lines.forEach((line, lineIndex) => {
                        const lineY = startY + (lineIndex * lineHeight);

                        // Shadow
                        ctx.fillText(line, 1 * scaleFactor, lineY + (1 * scaleFactor), maxTextWidth);
                    });

                    // Ana yazı
                    ctx.fillStyle = prize.textColor || this.getTextColorForBackground(prize.color || '#ffffff');
                    lines.forEach((line, lineIndex) => {
                        const lineY = startY + (lineIndex * lineHeight);
                        ctx.fillText(line, 0, lineY, maxTextWidth);
                    });
                } else {
                    // Tek satır

                    // Shadow
                    ctx.fillText(prize.name, 1 * scaleFactor, 1 * scaleFactor, maxTextWidth);

                    // Ana yazı
                    ctx.fillStyle = prize.textColor || this.getTextColorForBackground(prize.color || '#ffffff');
                    ctx.fillText(prize.name, 0, 0, maxTextWidth);
                }

                ctx.restore();

                // Add small icons or decorative elements
                const iconRadius = radius * 0.88;
                const iconX = centerX + Math.cos(textAngle) * iconRadius;
                const iconY = centerY + Math.sin(textAngle) * iconRadius;

                ctx.save();
                ctx.translate(iconX, iconY);
                ctx.fillStyle = prize.textColor || this.getTextColorForBackground(prize.color || '#ffffff');

                // Adaptif ikon boyutu - canvas boyutunun yüzdesi
                const iconSize = Math.max(Math.round(canvasSize * 0.045), 10); // Canvas boyutunun %4.5'i
                ctx.font = `${iconSize}px serif`;
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';

                // Prize type icons are removed - no icons displayed
                ctx.restore();
            });

            // Logo is now rendered as an HTML img overlay (not on canvas) for better quality
            // Only draw fallback center circle when there's no logo
            if (!hasLogo) {
                const primaryColor = this.config.primaryColor || '#f59e0b';
                const secondaryColor = this.config.secondaryColor || '#fef3c7';

                // Draw center circle with advanced gradient and shadow effects
                // Outer shadow for depth
                ctx.save();
                ctx.shadowColor = 'rgba(0, 0, 0, 0.4)';
                ctx.shadowBlur = 10 * scaleFactor;
                ctx.shadowOffsetY = 4 * scaleFactor;

                // Main center circle - gradient based on primary color
                const centerRadius = 20 * scaleFactor;
                const centerGradient = ctx.createRadialGradient(centerX, centerY, 0, centerX, centerY, centerRadius);
                centerGradient.addColorStop(0, secondaryColor); // Light center
                centerGradient.addColorStop(0.3, this.shadeColor(primaryColor, 20)); // Medium
                centerGradient.addColorStop(0.7, primaryColor); // Rich
                centerGradient.addColorStop(1, this.shadeColor(primaryColor, -20)); // Deep

                ctx.beginPath();
                ctx.arc(centerX, centerY, centerRadius, 0, 2 * Math.PI);
                ctx.fillStyle = centerGradient;
                ctx.fill();
                ctx.restore(); // Remove shadow

                // Metallic border
                const borderGradient = ctx.createLinearGradient(centerX - centerRadius, centerY - centerRadius, centerX + centerRadius, centerY + centerRadius);
                borderGradient.addColorStop(0, this.shadeColor(primaryColor, 40)); // Bright
                borderGradient.addColorStop(0.5, secondaryColor); // Light highlight
                borderGradient.addColorStop(1, this.shadeColor(primaryColor, -40)); // Dark shadow

                ctx.strokeStyle = borderGradient;
                ctx.lineWidth = 3 * scaleFactor;
                ctx.stroke();

                // Inner decorative ring
                const ringRadius = 12 * scaleFactor;
                const ringGradient = ctx.createRadialGradient(centerX, centerY, 0, centerX, centerY, ringRadius);
                ringGradient.addColorStop(0, secondaryColor); // Light center
                ringGradient.addColorStop(0.4, this.shadeColor(primaryColor, 20)); // Medium
                ringGradient.addColorStop(0.8, primaryColor); // Rich
                ringGradient.addColorStop(1, this.shadeColor(primaryColor, -30)); // Dark edge

                ctx.beginPath();
                ctx.arc(centerX, centerY, ringRadius, 0, 2 * Math.PI);
                ctx.fillStyle = ringGradient;
                ctx.fill();

                // Inner ring border
                ctx.strokeStyle = primaryColor;
                ctx.lineWidth = 1 * scaleFactor;
                ctx.stroke();

                // Center jewel effect
                const jewelRadius = 6 * scaleFactor;
                const jewelGradient = ctx.createRadialGradient(centerX, centerY, 0, centerX, centerY, jewelRadius);
                jewelGradient.addColorStop(0, '#ffffff'); // Almost white
                jewelGradient.addColorStop(0.2, secondaryColor); // Light
                jewelGradient.addColorStop(0.6, this.shadeColor(primaryColor, 20)); // Medium
                jewelGradient.addColorStop(0.9, primaryColor); // Rich
                jewelGradient.addColorStop(1, this.shadeColor(primaryColor, -20)); // Deep

                ctx.beginPath();
                ctx.arc(centerX, centerY, jewelRadius, 0, 2 * Math.PI);
                ctx.fillStyle = jewelGradient;
                ctx.fill();

                // Multiple highlight spots for crystal effect
                // Main highlight
                ctx.beginPath();
                ctx.arc(centerX - (2 * scaleFactor), centerY - (2 * scaleFactor), 2.5 * scaleFactor, 0, 2 * Math.PI);
                ctx.fillStyle = 'rgba(255, 255, 255, 0.95)'; // White highlight
                ctx.fill();

                // Secondary highlight
                ctx.beginPath();
                ctx.arc(centerX + (1 * scaleFactor), centerY - (3 * scaleFactor), 1.5 * scaleFactor, 0, 2 * Math.PI);
                ctx.fillStyle = 'rgba(255, 255, 255, 0.8)'; // White highlight
                ctx.fill();

                // Tiny sparkle
                ctx.beginPath();
                ctx.arc(centerX + (2 * scaleFactor), centerY + (1 * scaleFactor), 0.8 * scaleFactor, 0, 2 * Math.PI);
                ctx.fillStyle = 'rgba(255, 255, 255, 0.9)'; // White sparkle
                ctx.fill();
            }

            // Draw decorative dots around the wheel
            this.drawWheelDots(ctx, centerX, centerY, radius + (12 * scaleFactor));

            // Draw pointer on canvas
            this.drawWheelPointer(ctx, centerX, centerY);
        }

        shadeColor(color, percent) {
            // return color;
            if (!/^#?[0-9A-Fa-f]{6}$/.test(color)) {
                return color; // Geçersiz renk formatıysa orijinal rengi döndür
            }

            const hex = color.replace("#", "");
            const num = parseInt(hex, 16);
            const amt = Math.round(2.55 * percent);

            const clamp = (val) => Math.max(0, Math.min(255, val));

            const R = clamp((num >> 16) + amt);
            const G = clamp((num >> 8 & 0x00FF) + amt);
            const B = clamp((num & 0x0000FF) + amt);

            return `#${((1 << 24) + (R << 16) + (G << 8) + B)
                .toString(16)
                .slice(1)
                .toUpperCase()}`;
        }

        drawWheelPointer(ctx, centerX, centerY) {
            const canvas = this.canvas;
            const canvasSize = Math.min(canvas.width, canvas.height);
            const scaleFactor = canvasSize / 300;
            const radius = Math.min(centerX, centerY) - (15 * scaleFactor);

            // Pointer tam üstte, çarkın kenarında
            const pointerX = centerX;
            const pointerY = centerY - radius - (25 * scaleFactor); // Çarkın üstünde

            // Pointer gölgesi
            ctx.save();
            ctx.shadowColor = 'rgba(0, 0, 0, 0.3)';
            ctx.shadowBlur = 8 * scaleFactor;
            ctx.shadowOffsetY = 4 * scaleFactor;

            const secondaryColor = this.config.secondaryColor || '#ff6b6b';

            // Ana pointer şekli - üçgen benzeri ok
            ctx.beginPath();
            ctx.moveTo(pointerX, pointerY + (30 * scaleFactor)); // Uç (çarka doğru bakan)
            ctx.lineTo(pointerX - (12 * scaleFactor), pointerY); // Sol üst
            ctx.lineTo(pointerX + (12 * scaleFactor), pointerY); // Sağ üst
            ctx.closePath();

            // Gradient dolgu
            const pointerGradient = ctx.createLinearGradient(pointerX, pointerY, pointerX, pointerY + (30 * scaleFactor));
            pointerGradient.addColorStop(0, this.shadeColor(secondaryColor, 20));
            pointerGradient.addColorStop(0.5, secondaryColor);
            pointerGradient.addColorStop(1, this.shadeColor(secondaryColor, -20));

            ctx.fillStyle = pointerGradient;
            ctx.fill();

            // Beyaz kenarlık
            ctx.strokeStyle = '#ffffff';
            ctx.lineWidth = 3 * scaleFactor;
            ctx.stroke();

            ctx.restore();

            // İç parlak nokta (highlight)
            ctx.beginPath();
            ctx.arc(pointerX - (3 * scaleFactor), pointerY + (8 * scaleFactor), 3 * scaleFactor, 0, 2 * Math.PI);
            ctx.fillStyle = 'rgba(255, 255, 255, 0.7)';
            ctx.fill();

            // Pointer gövdesi (dikdörtgen)
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(pointerX - (3 * scaleFactor), pointerY - (5 * scaleFactor), 6 * scaleFactor, 8 * scaleFactor);
            ctx.strokeStyle = secondaryColor;
            ctx.lineWidth = 2 * scaleFactor;
            ctx.strokeRect(pointerX - (3 * scaleFactor), pointerY - (5 * scaleFactor), 6 * scaleFactor, 8 * scaleFactor);
        }

        drawWheelDots(ctx, centerX, centerY, radius) {
            const canvas = this.canvas;
            const canvasSize = Math.min(canvas.width, canvas.height);
            const scaleFactor = canvasSize / 300;
            const secondaryColor = this.config.secondaryColor || '#ffd700';

            const dotCount = 24;
            const dotRadius = 4 * scaleFactor;

            // Hex to RGB conversion for glow
            const hex = secondaryColor.replace('#', '');
            const r = parseInt(hex.substring(0, 2), 16);
            const g = parseInt(hex.substring(2, 4), 16);
            const b = parseInt(hex.substring(4, 6), 16);

            for (let i = 0; i < dotCount; i++) {
                const angle = (i * 360 / dotCount) * (Math.PI / 180);
                const x = centerX + Math.cos(angle) * radius;
                const y = centerY + Math.sin(angle) * radius;

                ctx.beginPath();
                ctx.arc(x, y, dotRadius + (2 * scaleFactor), 0, 2 * Math.PI);
                ctx.fillStyle = `rgba(${r}, ${g}, ${b}, 0.3)`;
                ctx.fill();

                const dotGradient = ctx.createRadialGradient(x, y, 0, x, y, dotRadius);
                dotGradient.addColorStop(0, this.shadeColor(secondaryColor, 20));
                dotGradient.addColorStop(0.5, secondaryColor);
                dotGradient.addColorStop(1, this.shadeColor(secondaryColor, -20));

                ctx.beginPath();
                ctx.arc(x, y, dotRadius, 0, 2 * Math.PI);
                ctx.fillStyle = dotGradient;
                ctx.fill();

                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.5 * scaleFactor;
                ctx.stroke();

                ctx.beginPath();
                ctx.arc(x - (1 * scaleFactor), y - (1 * scaleFactor), dotRadius * 0.4, 0, 2 * Math.PI);
                ctx.fillStyle = 'rgba(255, 255, 255, 0.6)';
                ctx.fill();
            }
        }

        getTextColorForBackground(hexColor) {
            const hex = hexColor.replace('#', '');
            const r = parseInt(hex.substr(0, 2), 16);
            const g = parseInt(hex.substr(2, 2), 16);
            const b = parseInt(hex.substr(4, 2), 16);

            const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

            return luminance > 0.5 ? '#000000' : '#ffffff';
        }

        setupFormValidation(modal) {
            const phoneInput = modal.querySelector('#phone-input');
            const nameInput = modal.querySelector('#name-input');
            const emailInput = modal.querySelector('#email-input');
            const consentCheckbox1 = modal.querySelector('#consent-checkbox-1');
            const consentCheckbox2 = modal.querySelector('#consent-checkbox-2');

            const clearError = (element) => {
                if (element.classList.contains('pushonica-input-error')) {
                    element.classList.remove('pushonica-input-error');
                }
                
                let parent = element.parentNode;
                let errorDiv = parent.querySelector('.pushonica-validation-error');
                
                if (!errorDiv && element.nextElementSibling && element.nextElementSibling.classList && element.nextElementSibling.classList.contains('pushonica-validation-error')) {
                    errorDiv = element.nextElementSibling;
                }

                if (errorDiv) {
                    errorDiv.remove();
                }
            };

            if (nameInput) {
                let warningMessage = document.createElement('div');
                warningMessage.style.color = '#ef4444';
                warningMessage.style.fontSize = '12px';
                warningMessage.style.marginTop = '4px';
                warningMessage.style.display = 'none';
                warningMessage.textContent = 'İsim Soyisim alanı yalnızca harflerden oluşmalıdır.';
                nameInput.parentNode.appendChild(warningMessage);

                nameInput.addEventListener('input', (e) => {
                    clearError(e.target);
                    let currentValue = e.target.value;
                    const invalidPattern = /[^a-zA-ZğüşıöçĞÜŞİÖÇ\s]/g;

                    if (invalidPattern.test(currentValue)) {
                        warningMessage.style.display = 'block';
                        currentValue = currentValue.replace(invalidPattern, '');
                    } else {
                        warningMessage.style.display = 'none';
                    }

                    const words = currentValue.split(' ');
                    const capitalized = words.map(word =>
                        word.charAt(0).toUpperCase() + word.slice(1).toLowerCase()
                    );
                    
                    e.target.value = capitalized.join(' ');
                });
            }

            if (phoneInput) {
                phoneInput.addEventListener('input', (e) => {
                    clearError(e.target);
                    let value = e.target.value.replace(/\D/g, '');

                    if (value.startsWith('90')) value = value.substring(2);
                    if (value.length > 10) value = value.substring(0, 10);

                    let formatted = '+90';
                    if (value.length > 0) formatted += ' ' + value.substring(0, 3);
                    if (value.length > 3) formatted += ' ' + value.substring(3, 6);
                    if (value.length > 6) formatted += ' ' + value.substring(6, 8);
                    if (value.length > 8) formatted += ' ' + value.substring(8, 10);

                    e.target.value = formatted;
                });
            }

            if (emailInput) {
                emailInput.addEventListener('input', (e) => {
                    clearError(e.target);
                });
            }

            // Checkboxes event listeners to clear error on consent group
            const clearConsentError = () => {
                const consentGroup = modal.querySelector('.pushonica-consent-group');
                if (consentGroup) {
                    const errorDiv = consentGroup.querySelector('.pushonica-validation-error');
                    if (errorDiv) errorDiv.remove();
                }
            };

            if (consentCheckbox1) {
                consentCheckbox1.addEventListener('change', clearConsentError);
            }
            if (consentCheckbox2) {
                consentCheckbox2.addEventListener('change', clearConsentError);
            }
        }

        async handleSpin() {
            if (this.isSpinning) return;

            this.stopIdleAnimation();

            const modal = document.getElementById('pushonica-wheel-modal') || this.targetElement;
            const spinBtn = modal.querySelector('#spin-btn');
            const userForm = modal.querySelector('#user-form');
            const resultDiv = modal.querySelector('#result-message');

            if (!this.options.livePreview && !this.validateForm(modal)) {
                this.startIdleAnimation();
                return;
            }

            this.isSpinning = true;
            if (spinBtn) {
                spinBtn.disabled = true;
                spinBtn.textContent = 'Çark Dönüyor...';
            }
            if (resultDiv) resultDiv.style.display = 'none';

            try {
                const userData = this.getUserData(modal);

                const spinResult = await this.callSpinAPI(userData);

                if (!spinResult.success) {
                    this.showSpinResult(spinResult, resultDiv);
                    return;
                }

                await this.animateWheel(spinResult);

                // Show result if livePreview (admin test) OR regular mode (not livePreview)
                // If livePreview was hiding it, now we show it because user wants to see result in test
                if (true) { 
                    this.showSpinResult(spinResult, resultDiv);
                }
                this.updateSpinCookie();

                if (userForm) userForm.style.display = 'none';
                if (spinBtn) spinBtn.style.display = 'none';

                const isEligible = await this.checkEligibility();
                if (isEligible) {
                    setTimeout(() => {
                        const retryBtn = document.createElement('button');
                        retryBtn.className = 'pushonica-spin-button';
                        retryBtn.textContent = 'Tekrar Çevir';
                        retryBtn.style.marginTop = '10px';

                        retryBtn.addEventListener('click', () => {
                            if (userForm) userForm.style.display = 'block';
                            if (spinBtn) spinBtn.style.display = 'block';
                            retryBtn.remove();
                            if (resultDiv) resultDiv.style.display = 'none';
                        });

                        if (resultDiv && resultDiv.parentNode) {
                            resultDiv.parentNode.insertBefore(retryBtn, resultDiv.nextSibling);
                        }
                    }, 500);
                }

            } catch (error) {
                console.error('[GiftWheel] Spin failed:', error);
                const errorResult = {
                    success: false,
                    won: false,
                    prize: { prizeType: 'none', name: 'Hata' },
                    message: 'Bir hata oluştu, lütfen tekrar deneyin.'
                };
                this.showSpinResult(errorResult, resultDiv);
            } finally {
                this.isSpinning = false;
                if (spinBtn) {
                    spinBtn.disabled = false;
                    spinBtn.textContent = this.config.buttonText || 'Çarkı Çevir';
                }
                this.startIdleAnimation();
            }
        }

        startIdleAnimation() {
            if (!this.config.logoAnimation) return;
            // Apply CSS pulse animation to the HTML logo overlay
            if (this.logoOverlayEl) {
                this.logoOverlayEl.style.animation = 'pushonica-logo-pulse 2s ease-in-out infinite';
            }
        }

        stopIdleAnimation() {
            if (this.logoOverlayEl) {
                this.logoOverlayEl.style.animation = 'none';
            }
        }

        validateForm(modal) {
            this.clearErrors(modal);
            let isValid = true;
            let firstInvalid = null;

            const nameInput = modal.querySelector('#name-input');
            if (!nameInput || !nameInput.value.trim()) {
                this.showError(nameInput, 'Lütfen ad soyadınızı giriniz.');
                if (!firstInvalid && nameInput) firstInvalid = nameInput;
                isValid = false;
            }

            if (this.config.requirePhone) {
                const phoneInput = modal.querySelector('#phone-input');
                let isPhoneValid = true;
                
                if (!phoneInput || !phoneInput.value.trim() || phoneInput.value === '+90') {
                    this.showError(phoneInput, 'Lütfen geçerli bir telefon numarası giriniz.');
                    isPhoneValid = false;
                } else {
                    const phoneValue = phoneInput.value.replace(/\D/g, '');
                    if (phoneValue.length < 12) {
                        this.showError(phoneInput, 'Lütfen geçerli bir telefon numarası giriniz.');
                        isPhoneValid = false;
                    }
                }

                if (!isPhoneValid) {
                    if (!firstInvalid && phoneInput) firstInvalid = phoneInput;
                    isValid = false;
                }
            }

            if (this.config.requireEmail) {
                const emailInput = modal.querySelector('#email-input');
                let isEmailValid = true;

                if (!emailInput || !emailInput.value.trim()) {
                    this.showError(emailInput, 'Lütfen e-posta adresinizi giriniz.');
                    isEmailValid = false;
                } else {
                    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
                    if (!emailRegex.test(emailInput.value.trim())) {
                        this.showError(emailInput, 'Lütfen geçerli bir e-posta adresi giriniz.');
                        isEmailValid = false;
                    }
                }

                if (!isEmailValid) {
                    if (!firstInvalid && emailInput) firstInvalid = emailInput;
                    isValid = false;
                }
            }

            if (this.config.requirePhone || this.config.requireEmail) {
                const consentCheckbox1 = modal.querySelector('#consent-checkbox-1');
                const consentCheckbox2 = modal.querySelector('#consent-checkbox-2');
                const consentGroup = modal.querySelector('.pushonica-consent-group');

                if ((consentCheckbox1 && !consentCheckbox1.checked) || (consentCheckbox2 && !consentCheckbox2.checked)) {
                    if (consentGroup) {
                        this.showError(consentGroup.lastElementChild, 'Lütfen tüm izinleri onaylayınız.');
                    }
                    if (!firstInvalid) {
                        if (consentCheckbox1 && !consentCheckbox1.checked) firstInvalid = consentCheckbox1;
                        else if (consentCheckbox2) firstInvalid = consentCheckbox2;
                    }
                    isValid = false;
                }
            }

            if (firstInvalid) firstInvalid.focus();
            return isValid;
        }

        showError(element, message) {
            if (!element) return;
            
            if (element.classList.contains('pushonica-form-input')) {
                element.classList.add('pushonica-input-error');
            }
            
            let parent = element.parentNode;
            
            if (element.classList.contains('pushonica-checkbox-label')) {
            }

            let errorDiv = parent.querySelector('.pushonica-validation-error');
            
            if (!errorDiv && element.nextElementSibling && element.nextElementSibling.classList && element.nextElementSibling.classList.contains('pushonica-validation-error')) {
                errorDiv = element.nextElementSibling;
            }

            if (!errorDiv) {
                errorDiv = document.createElement('div');
                errorDiv.className = 'pushonica-validation-error';
                
                if (element.classList.contains('pushonica-checkbox-label') || element.tagName === 'LABEL') {
                   parent.appendChild(errorDiv);
                } else {
                   parent.appendChild(errorDiv);
                }
            }
            
            errorDiv.textContent = message;
            errorDiv.style.display = 'block';
        }

        clearErrors(modal) {
            const errors = modal.querySelectorAll('.pushonica-validation-error');
            errors.forEach(el => el.remove());
            const inputs = modal.querySelectorAll('.pushonica-input-error');
            inputs.forEach(el => el.classList.remove('pushonica-input-error'));
        }

        getUserData(modal) {
            const data = {};

            const nameInput = modal.querySelector('#name-input');
            if (nameInput) {
                data.customerName = nameInput.value.trim();
            }

            if (this.config.requirePhone) {
                const phoneInput = modal.querySelector('#phone-input');
                if (phoneInput) {
                    const phoneValue = phoneInput.value.replace(/\D/g, '');
                    // Format phone as expected by API
                    data.customerPhone = phoneValue.startsWith('90') ? '+' + phoneValue : '+90' + phoneValue;
                }
            }

            if (this.config.requireEmail) {
                const emailInput = modal.querySelector('#email-input');
                if (emailInput) {
                    data.customerEmail = emailInput.value;
                }
            }

            if (this.config.requirePhone || this.config.requireEmail) {
                const consentCheckbox1 = modal.querySelector('#consent-checkbox-1');
                const consentCheckbox2 = modal.querySelector('#consent-checkbox-2');
                data.communicationConsent = (consentCheckbox1 && consentCheckbox1.checked && consentCheckbox2 && consentCheckbox2.checked);
            }

            return data;
        }

        async callSpinAPI(userData) {
            // Enable mock logic for both explicit testMode AND livePreview (admin panel testing)
            if (this.testMode || this.options.livePreview) {
                return new Promise((resolve) => {
                    setTimeout(() => {
                        const prizes = this.config.prizes.filter(p => p.isActive !== false);
                        if (!prizes.length) {
                             resolve(this.formatAPIResponse({ success: false, message: 'Aktif ödül bulunamadı' }));
                             return;
                        }

                        let totalProb = 0;
                        prizes.forEach(p => totalProb += (p.probability || 0));
                        
                        let random = Math.random() * totalProb;
                        let selectedPrize = prizes[0];

                        for (let i = 0; i < prizes.length; i++) {
                            if (random < prizes[i].probability) {
                                selectedPrize = prizes[i];
                                break;
                            }
                            random -= prizes[i].probability;
                        }

                        // Determine win status based on prize type
                        const pType = (selectedPrize.prizeType || '').toLowerCase();
                        
                        // Check if it's a "Try Again" / "Lose" result
                        const isTryAgain = pType === 'none' || selectedPrize.name.toLowerCase().includes('tekrar dene');
                        const isWin = !isTryAgain && pType !== 'error';
                        
                        const winTemplate = this.config.winMessage || "Tebrikler! {prize} kazandınız!";
                        const loseTemplate = this.config.loseMessage || "Bu sefer olmadı, tekrar deneyin!";

                        // Generate mock voucher code:
                        // 1. If type is voucher
                        // 2. OR if it's a win and NOT a message type (fallback)
                        let voucherCode = null;
                        if (pType === 'voucher' || (isWin && pType !== 'message')) {
                            const prefix = selectedPrize.voucherPrefix || 'TEST';
                            const randomPart = Math.random().toString(36).substr(2, 6).toUpperCase();
                            voucherCode = `${prefix}-${randomPart}`;
                        }

                        const result = {
                            success: true,
                            prize: selectedPrize,
                            won: isWin,
                            voucherCode: voucherCode,
                            message: isWin ? winTemplate.replace('{prize}', selectedPrize.name) : loseTemplate
                        };
                        
                        resolve(this.formatAPIResponse(result));
                    }, 800);
                });
            }

            try {
                const response = await fetch(`${this.apiBaseUrl}/api/gift-wheel/${this.companyId}/spin`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(userData)
                });

                const result = await response.json();

                if (!response.ok) {
                    return {
                        success: false,
                        message: result.message || 'Bir hata oluştu, lütfen tekrar deneyin.',
                        error: result.error || 'API_ERROR',
                        wheelId: this.config.wheelId
                    };
                }

                // Format the API response for our wheel system
                return this.formatAPIResponse(result);
            } catch (error) {
                return {
                    success: false,
                    message: 'Bağlantı hatası, lütfen tekrar deneyin.',
                    error: 'CONNECTION_ERROR',
                    wheelId: this.config.wheelId
                };
            }
        }

        formatAPIResponse(apiResult) {
            if (!apiResult.success) {
                const errorResult = {
                    success: false,
                    won: false,
                    prize: { prizeType: 'error', name: 'Hata' },
                    message: apiResult.message || 'Bir hata oluştu',
                    targetAngle: 0,
                    voucherCode: null
                };
                return errorResult;
            }

            const prize = apiResult.prize;
            if (!prize) {
                return {
                    success: false,
                    won: false,
                    prize: null,
                    message: 'Ödül bilgisi bulunamadı',
                    targetAngle: 0,
                    voucherCode: null
                };
            }

            const prizes = this.config.prizes.filter(p => p.isActive);
            const prizeIndex = prizes.findIndex(p => p.id === prize.id);
            const segmentAngle = 360 / prizes.length;

            const safeMargin = 0.1; // Segment'in %10'u kenar koruması
            const safeRange = 1 - (2 * safeMargin); // %80 kullanılabilir alan
            const randomOffset = (Math.random() * safeRange) * segmentAngle;

            let baseAngle = (prizeIndex * segmentAngle);
            baseAngle += randomOffset;

            const normalizedCurrentRotation = this.currentRotation % 360;
            let targetAngle = baseAngle + normalizedCurrentRotation;

            if (targetAngle < 0) {
                targetAngle += 360;
            }

            const pType = (prize.prizeType || '').toLowerCase();
            // Also check name fallback if type is strictly checked elsewhere but here we want robustness
            // message tipi de "tekrar deneyin" gibi ödüller için kullanılıyor
            const isTryAgain = pType === 'none' || pType === 'message' || prize.name.toLowerCase().includes('tekrar dene');
            const isWin = !isTryAgain && pType !== 'error';

            let resultMessage = '';
            if (pType === 'voucher' || pType === 'cargo') {
                resultMessage = this.config.winMessage.replace('{prize}', prize.name);
            } else if (pType === 'message') {
                // message tipi ödüller için (Tekrar Deneyin vb.) loseMessage
                resultMessage = this.config.loseMessage || prize.name;
            } else if (pType === 'none') {
                resultMessage = this.config.loseMessage || prize.name;
            } else {
                resultMessage = apiResult.message || this.config.loseMessage || prize.name;
            }

            return {
                success: true,
                won: isWin,
                prize: prize,
                message: resultMessage,
                targetAngle: (360 - targetAngle),
                voucherCode: apiResult.voucherCode || null
            };
        }

        async animateWheel(result) {
            const duration = 5000; // 5 seconds for more drama with ease-out
            const startTime = Date.now();
            const startRotation = this.currentRotation;
            const baseRotations = 5; // 5 full rotations
            const targetRotation = startRotation + (baseRotations * 360) + (result.targetAngle || 0);
            
            const prizes = this.config.prizes.filter(p => p.isActive);
            const segmentAngle = 360 / prizes.length;
            let lastSegmentIndex = Math.floor((startRotation % 360) / segmentAngle);

            return new Promise((resolve) => {
                const animate = () => {
                    const elapsed = Date.now() - startTime;
                    const progress = Math.min(elapsed / duration, 1);

                    // ease-out-quart: 1 - (1 - t)^4
                    const easeOut = 1 - Math.pow(1 - progress, 4);

                    this.currentRotation = startRotation + (targetRotation - startRotation) * easeOut;
                    
                    const currentNormalizedRotation = this.currentRotation % 360;
                    const currentSegmentIndex = Math.floor(currentNormalizedRotation / segmentAngle);
                    
                    if (currentSegmentIndex !== lastSegmentIndex && progress < 0.98) {
                        this.playTickSound();
                        lastSegmentIndex = currentSegmentIndex;
                    }

                    this.drawWheel();

                    if (progress < 1) {
                        requestAnimationFrame(animate);
                    } else {
                        this.currentRotation = this.currentRotation % 360;
                        if (this.currentRotation < 0) this.currentRotation += 360;

                        this.drawWheel();
                        
                        const winPType = (result.prize && result.prize.prizeType || '').toLowerCase();
                        if (result.success && result.won && (result.voucherCode || (winPType !== 'message' && winPType !== 'voucher'))) {
                            setTimeout(() => this.playWinSound(), 200);
                        }
                        
                        resolve();
                    }
                };

                animate();
            });
        }

        showSpinResult(result, resultDiv) {
            if (!resultDiv) {
                console.warn('[GiftWheel] Result container not found', result);
                return;
            }

            let className = 'pushonica-result-message';
            const pType = (result.prize && result.prize.prizeType || '').toLowerCase();

            // Determine style class based on outcome
            if (result.success && (result.won || result.voucherCode)) {
                if (pType === 'message') {
                    className += ' pushonica-message';
                } else {
                    className += ' pushonica-win';
                }
            } else {
                if (pType === 'none' || (result.prize && result.prize.name && result.prize.name.toLowerCase().includes('tekrar'))) {
                    className += ' pushonica-retry';
                } else {
                    className += ' pushonica-lose';
                }
            }

            resultDiv.className = className;

            let message = result.message || 'Sonuç yükleniyor...'; // Fallback mesaj

            // Special handling for Vouchers with Code - Relaxed condition
            if (result.success && result.voucherCode) {
                const voucherCode = result.voucherCode;
                const discountInfo = result.prize.discountAmount ? 
                    (result.prize.discountType === 1 ? `${result.prize.discountAmount} TL` : `%${result.prize.discountAmount}`) 
                    : '';
                
                const validity = result.prize.validityDays ? `${result.prize.validityDays} gün geçerlidir` : '';

                let content = `
                    <div class="pushonica-voucher-display">
                        <div class="pushonica-voucher-label">Hediye Çeki Kodunuz</div>
                        <div class="pushonica-voucher-box" id="pushonica-voucher-box-target" title="Kodu Kopyala">
                            <span class="pushonica-voucher-text">${voucherCode}</span>
                            <button class="pushonica-copy-btn" id="pushonica-copy-btn-action" type="button">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                                    <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
                                </svg>
                            </button>
                            <div class="pushonica-copy-feedback" id="pushonica-copy-feedback" style="opacity:0;">Kopyalandı!</div>
                        </div>
                `;

                if (discountInfo) {
                     content += `<div class="pushonica-voucher-discount">İndirim: ${discountInfo}</div>`;
                }

                if (validity) {
                     content += `<div class="pushonica-voucher-validity">${validity}</div>`;
                }

                // Show original message as small info
                content += `<div class="pushonica-voucher-info-text">${message}</div>`;
                
                content += `</div>`;
                
                resultDiv.innerHTML = content;
                
                // Add click handler for copy (using timeout to ensure DOM is ready)
                setTimeout(() => {
                    const copyBtn = resultDiv.querySelector('#pushonica-copy-btn-action');
                    const voucherBox = resultDiv.querySelector('#pushonica-voucher-box-target');
                    const feedback = resultDiv.querySelector('#pushonica-copy-feedback');
                    const copyAction = (e) => {
                        if(e) e.stopPropagation();
                        
                        if (navigator.clipboard) {
                            navigator.clipboard.writeText(voucherCode).then(() => {
                                if (feedback) {
                                    feedback.style.animation = 'none';
                                    feedback.offsetHeight; /* trigger reflow */
                                    feedback.style.animation = 'pushonica-tooltip-pop 0.3s forwards';
                                    
                                    setTimeout(() => {
                                        feedback.style.animation = 'pushonica-tooltip-fade 0.3s forwards';
                                    }, 1500);
                                }
                            }).catch(err => {
                                // Fallback or ignore
                            });
                        }
                    };

                    if (copyBtn) copyBtn.addEventListener('click', copyAction);
                    if (voucherBox) voucherBox.addEventListener('click', copyAction);
                }, 10);

            } else {
                if (result.success && pType === 'voucher') {
                    if (result.voucherCode) {
                        message += `<br><strong>Kupon Kodu: ${result.voucherCode}</strong>`;
                    }
                    if (result.prize.discountAmount) {
                        if (result.prize.discountType === 1) {
                            message += `<br><strong>İndirim: ${result.prize.discountAmount} TL</strong>`;
                        } else if (result.prize.discountType === 2) {
                            message += `<br><strong>İndirim: %${result.prize.discountAmount}</strong>`;
                        }
                    }
                    if (result.prize.validityDays) {
                        message += `<br><small>${result.prize.validityDays} gün geçerlidir</small>`;
                    }
                }

                // "Tekrar Dene" veya "message" tipi için özel stil
                if ((pType === 'none' || pType === 'message') && result.prize.name) {
                    message = this.config.loseMessage || result.prize.name;
                }

                resultDiv.innerHTML = message;
            }

            resultDiv.style.display = 'block';

            const confettiPType = (result.prize && result.prize.prizeType || '').toLowerCase();
            const shouldConfetti = result.success && result.won && (result.voucherCode || (confettiPType !== 'message' && confettiPType !== 'voucher'));
            if (shouldConfetti && this.config.showConfetti !== false) {
                this.showConfetti();
            }
        }

        showConfetti() {
            // Create more confetti particles for better effect
            for (let i = 0; i < 120; i++) {
                setTimeout(() => {
                    const isEmoji = Math.random() < 0.3; // 30% chance for emoji
                    const confetti = document.createElement('div');

                    if (isEmoji) {
                        confetti.className = 'pushonica-confetti-emoji';
                        const emojis = ['🎁', '⭐', '🌟', '✨', '🎉', '🎊', '💫'];
                        confetti.textContent = emojis[Math.floor(Math.random() * emojis.length)];

                        const size = Math.random() * 10 + 15; // 15px to 25px
                        confetti.style.fontSize = size + 'px';
                    } else {
                        confetti.className = 'pushonica-confetti';

                        const colors = [
                            '#ff6b6b', '#4ecdc4', '#45b7d1', '#96ceb4', '#feca57',
                            '#ff9ff3', '#54a0ff', '#5f27cd', '#00d2d3', '#ff9f43',
                            '#10ac84', '#ee5a24', '#0abde3', '#c44569', '#6c5ce7',
                            '#a29bfe', '#fd79a8', '#e17055', '#00b894', '#fdcb6e'
                        ];
                        confetti.style.background = colors[Math.floor(Math.random() * colors.length)];

                        // Random size for circles
                        const size = Math.random() * 8 + 5; // 5px to 13px
                        confetti.style.width = size + 'px';
                        confetti.style.height = size + 'px';

                        // Different shapes
                        const shapes = ['50%', '0%', '10%']; // circle, square, rounded square
                        confetti.style.borderRadius = shapes[Math.floor(Math.random() * shapes.length)];
                    }

                    confetti.style.left = Math.random() * 100 + 'vw';
                    confetti.style.top = '-30px';

                    confetti.style.animationDelay = Math.random() * 1 + 's';
                    confetti.style.animationDuration = (Math.random() * 2 + 2) + 's'; // 2-4 seconds

                    confetti.style.transform = `rotate(${Math.random() * 360}deg)`;

                    document.body.appendChild(confetti);

                    setTimeout(() => {
                        if (confetti.parentNode) {
                            confetti.remove();
                        }
                    }, 5000);
                }, i * 25);
            }
        }

        updateSpinCookie() {
            const cookieData = this.getCookieData() || { spinCount: 0, lastSpinDate: new Date().toISOString() };
            const today = new Date().toDateString();
            const lastSpinDate = new Date(cookieData.lastSpinDate).toDateString();

            if (today === lastSpinDate) {
                cookieData.spinCount += 1;
            } else {
                cookieData.spinCount = 1;
                cookieData.lastSpinDate = new Date().toISOString();
            }

            this.setCookie(cookieData);
        }
    }

    // Expose class globally for admin usage
    window.PushonicaGiftWheel = PushonicaGiftWheel;

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            window.pushonicaGiftWheel = new PushonicaGiftWheel();
        });
    } else {
        window.pushonicaGiftWheel = new PushonicaGiftWheel();
    }

})();