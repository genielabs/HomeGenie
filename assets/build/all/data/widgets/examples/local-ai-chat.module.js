(async () => {
    const cid = 'examples-local-ai-chat';
    const moduleUrl = new URL(import.meta.url);
    const baseUrl = moduleUrl.href.substring(0, moduleUrl.href.lastIndexOf('/') + 1);

    // Extract 'mode' from URL search parameters, default to 'closed'
    const shadowMode = moduleUrl.searchParams.get('mode') || 'closed';

    // Gather all URL parameters as options
    const params = Object.fromEntries(moduleUrl.searchParams.entries());

    const register = () => {
        if (!customElements.get(cid)) {
            customElements.define(cid, class extends HTMLElement {
                connectedCallback() {
                    zuix.loadComponent(this,
                        baseUrl + 'local-ai-chat',
                        undefined,
                        {
                            ...params,
                            container: this.attachShadow({ mode: shadowMode })
                        }
                    );
                }
            });
        }
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
