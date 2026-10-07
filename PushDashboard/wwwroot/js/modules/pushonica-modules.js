/**
 * Pushonica Modules - Ana Modül Sistemi
 * Tüm Pushonica modüllerini yöneten ana script
 * Mevcut modülleri dinamik olarak yükler ve yönetir
 */

// ReklamKaynagi cookie setter from 'push' query param
(function () {
    function getQueryParam(param) {
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(param);
    }

    function setCookie(name, value, days) {
        let expires = "";
        if (days) {
            const date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + encodeURIComponent(value) + expires + "; path=/";
    }

    const pushValue = getQueryParam("push");
    if (pushValue) {
        const cookieValue = "kaynak=" + pushValue;
        setCookie("ReklamKaynagi", cookieValue, 30); // 30 gün saklar
    }

    'use strict';

    const PUSHONICA_VERSION = '1.0.0';

    class PushonicaModules {
        constructor() {
            this.baseUrl = this.determineBaseUrl();
            this.companyId = this.getCompanyId();
            this.modules = {};
            this.loadedScripts = new Set();
            this.initialized = false;
            this.debug = this.getDebugMode();
            this.domAnalyzer = null;

            this.log('Pushonica Modules initializing...', {
                companyId: this.companyId,
                baseUrl: this.baseUrl
            });
        }

        determineBaseUrl() {
            const scripts = document.querySelectorAll('script');
            let currentScript = document.currentScript;

            if (!currentScript) {
                for (let script of scripts) {
                    if (script.src && script.src.includes('pushonica-modules.js')) {
                        currentScript = script;
                        break;
                    }
                }
            }

            if (currentScript && currentScript.src) {
                const url = currentScript.src;
                return url.substring(0, url.lastIndexOf('/'));
            }

            return 'https://pub-059a933fce7c4fd4a28246282e53b076.r2.dev';
        }

        getCompanyId() {
            const scripts = document.querySelectorAll('script[data-module-id]');
            for (let script of scripts) {
                const companyId = script.getAttribute('data-module-id');
                if (companyId) return companyId;
            }

            const templateId = '{{COMPANY_ID}}';
            if (templateId !== '{{COMPANY_ID}}') return templateId;

            const urlParams = new URLSearchParams(window.location.search);
            return urlParams.get('pushonicaCompanyId') || null;
        }

        getDebugMode() {
            return window.location.search.includes('pushonicaDebug=true');
        }

        log(message, data = null) {
            if (this.debug) {
                console.log(`[Pushonica] ${message}`, data || '');
            }
        }

        async init() {
            if (this.initialized) return;

            if (!this.companyId) {
                this.log('Company ID not found. Pushonica modules will not load.');
                return;
            }

            try {
                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', () => this.loadModules());
                } else {
                    await this.loadModules();
                }

                this.initialized = true;
                this.log('Pushonica Modules initialized successfully');
            } catch (error) {
                this.log('Pushonica Modules initialization failed:', error);
            }
        }

        async loadModules() {
            try {
                await this.loadDOMAnalyzer();

                const pageType = this.domAnalyzer.analyzePageType();
                const pageInfo = this.domAnalyzer.getPageInfo();
                this.log('Detected page info:', pageInfo);

                const modulesList = await this.fetchModulesList();
                if (!modulesList || !modulesList.modules) {
                    this.log('No modules list found');
                    return;
                }

                for (const moduleConfig of modulesList.modules) {
                    if (this.shouldLoadModule(moduleConfig, pageType)) {
                        await this.tryLoadModule(moduleConfig.name);
                    }
                }

                this.log('All modules processed successfully');
            } catch (error) {
                this.log('Failed to load modules:', error);
            }
        }

        async loadDOMAnalyzer() {
            if (this.domAnalyzer) return;

            if (typeof TicimaxDOMAnalyzer !== 'undefined') {
                this.domAnalyzer = new TicimaxDOMAnalyzer();
                this.log('DOM Analyzer loaded from existing script');
                return;
            }

            try {
                await this.loadScript('ticimax-dom-analyzer.js');

                if (typeof TicimaxDOMAnalyzer !== 'undefined') {
                    this.domAnalyzer = new TicimaxDOMAnalyzer();
                    this.log('DOM Analyzer loaded successfully');
                } else {
                    throw new Error('TicimaxDOMAnalyzer class not found after loading script');
                }
            } catch (error) {
                this.log('Failed to load DOM Analyzer:', error);
                throw error;
            }
        }

        async fetchModulesList() {
            const urls = [`${this.baseUrl}/modules.json`];

            if (!this.baseUrl.startsWith('file://') && !this.baseUrl.includes('localhost')) {
                urls.push('./modules.json');
            }

            for (const url of urls) {
                try {
                    this.log('Fetching modules list from:', url);
                    const response = await fetch(url);

                    if (response.ok) {
                        const data = await response.json();
                        this.log('Modules list fetched successfully from:', url);
                        return data;
                    }
                    this.log(`HTTP error fetching from ${url}: ${response.status}`);
                } catch (error) {
                    this.log(`Network error fetching from ${url}:`, error);
                }
            }

            this.log('Failed to fetch modules list from all sources');
            return null;
        }

        shouldLoadModule(moduleConfig, pageType) {
            const moduleName = moduleConfig.name;

            if (moduleConfig.pages && !moduleConfig.pages.includes(pageType) && !moduleConfig.pages.includes('all')) {
                this.log(`Module ${moduleName} not allowed on page type: ${pageType}`);
                return false;
            }

            this.log(`Module ${moduleName} should be loaded on ${pageType}`);
            return true;
        }

        async tryLoadModule(moduleName) {
            try {
                this.log(`Trying to load module: ${moduleName}`);

                const scriptPath = `${this.companyId}/${moduleName}.js`;

                await this.loadScript(scriptPath);

                this.log(`Module ${moduleName} loaded successfully`);
                this.modules[moduleName] = {
                    loaded: true,
                    loadedAt: new Date().toISOString()
                };

            } catch (error) {
                this.log(`Failed to load module ${moduleName}:`, error);
            }
        }

        async loadScript(scriptPath) {
            if (this.loadedScripts.has(scriptPath)) {
                this.log(`Script already loaded: ${scriptPath}`);
                return;
            }

            const urls = [];

            if (scriptPath.startsWith('./')) {
                urls.push(scriptPath);
            } else {
                urls.push(`${this.baseUrl}/${scriptPath}`);

                if (!this.baseUrl.includes(scriptPath.split('/')[0])) {
                    urls.push(`./${scriptPath}`);
                }

                if (scriptPath.includes('/') && !scriptPath.startsWith('./')) {
                    const flatName = scriptPath.split('/').pop();
                    urls.push(`./${flatName}`);
                }
            }

            for (const url of urls) {
                try {
                    await this.injectScript(url);
                    this.loadedScripts.add(scriptPath);
                    this.log(`Script loaded successfully: ${url}`);
                    return;
                } catch (e) {
                    this.log(`Failed to load script from ${url}: ${e.message}`);
                }
            }

            throw new Error(`Failed to load script ${scriptPath} from all sources`);
        }

        injectScript(url) {
            return new Promise((resolve, reject) => {
                const script = document.createElement('script');
                script.src = url;
                script.onload = () => resolve();
                script.onerror = () => reject(new Error(`Script load error for ${url}`));
                document.head.appendChild(script);
            });
        }

        getModule(moduleName) {
            return this.modules[moduleName] || null;
        }

        isModuleLoaded(moduleName) {
            return this.modules[moduleName] && this.modules[moduleName].loaded;
        }

        isModuleAvailable(moduleName) {
            return this.modules[moduleName] && this.modules[moduleName].loaded;
        }

        getDebugInfo() {
            return {
                version: PUSHONICA_VERSION,
                companyId: this.companyId,
                initialized: this.initialized,
                loadedScripts: Array.from(this.loadedScripts),
                modules: Object.keys(this.modules),
                pageType: this.domAnalyzer ? this.domAnalyzer.analyzePageType() : 'unknown'
            };
        }
    }

    window.pushonicaModules = new PushonicaModules();

    if (window.pushonicaModules.debug) {
        window.PushonicaDebug = {
            getInfo: () => window.pushonicaModules.getDebugInfo(),
            getModule: (name) => window.pushonicaModules.getModule(name),
            reload: () => {
                window.pushonicaModules.initialized = false;
                window.pushonicaModules.init();
            }
        };
    }

    window.pushonicaModules.init();

})();
