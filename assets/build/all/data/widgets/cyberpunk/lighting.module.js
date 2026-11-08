(async () => {
    const cid = 'cyberpunk-lighting';
    const moduleUrl = new URL(import.meta.url);
    const baseUrl = moduleUrl.href.substring(0, moduleUrl.href.lastIndexOf('/') + 1);
    const shadowMode = moduleUrl.searchParams.get('mode') === 'open' ? 'open' : 'closed';
    const params = Object.fromEntries(moduleUrl.searchParams.entries());

    const register = () => {
        if (customElements.get(cid)) return;
        customElements.define(cid, class extends HTMLElement {
            connectedCallback() {
                // Moving an existing element must not recreate its controller or ShadowRoot.
                if (this.widgetMounted) return;
                this.widgetMounted = true;
                this.widgetOptions = this.widgetOptions || Array.from(this.attributes)
                    .filter(a => a.name.startsWith(':')).map(a => [a.name, a.value]);
                this.widgetOptions.forEach(([name, value]) => this.setAttribute(name, value));
                this.widgetRoot = this.widgetRoot || this.attachShadow({mode: shadowMode});
                const loader = customElements.get('zx-widget');
                this.widgetBridge = loader?.mount?.(this, baseUrl + 'lighting', {...params}, this.widgetRoot);
                if (!this.widgetBridge) {
                    zuix.loadComponent(this, baseUrl + 'lighting', undefined,
                        {...params, container: this.widgetRoot});
                }
            }
            disconnectedCallback() {
                queueMicrotask(() => {
                    if (this.isConnected || !this.widgetMounted) return;
                    this.widgetMounted = false;
                    this.widgetBridge?.dispose();
                    this.widgetBridge = undefined;
                    zuix.unload(this);
                    this.widgetRoot.replaceChildren();
                });
            }
        });
    };

    if (typeof self.zuix === 'undefined') {
        try {
            await import('https://cdn.jsdelivr.net/npm/zuix-dist@1.2.7/js/zuix.module.min.js');
            register();
        } catch (err) {
            console.error('Error loading zuix:', err);
        }
    } else {
        register();
    }
})();
