class SecurityKey extends HTMLElement {
    static get observedAttributes() {
        return ['register', 'login'];
    }

    constructor() {
        // Always call super first in constructor
        super();

        const shadow = this.attachShadow({ mode: 'open' });

        const linkBootstrap = $('<link>', {
            rel: 'stylesheet',
            href: '/css/bundle.css'
        }).appendTo(shadow);

        const container = $('<div>', {
            id: 'keyContainer',
            class: 'w-100 h-100 shadow',
        });

        const button = $('<button>', {
            id: 'registerKey',
            class: 'btn btn-primary',
        }).appendTo(container);

        container.appendTo(shadow)
    }

    connectedCallback() {
        const elem = this;
        const shadow = elem.shadowRoot;

        $(shadow).find('#registerKey').click(function () {
            registerKey(elem);
        })
    }

    disconnectedCallback() {
        console.log('Custom square element removed from page.');
    }

    adoptedCallback() {
        console.log('Custom square element moved to new page.');
    }

    attributeChangedCallback(name, oldValue, newValue) {

    }
}

function registerKey(elem) {
    const publicKeyCredentialCreationOptions = {
        challenge: Uint8Array.from(
            $(elem).attr('key'), c => c.charCodeAt(0)),
        rp: {
            name: "Leaf2Google",
            id: $(elem).attr('url'),
        },
        user: {
            id: Uint8Array.from(
                $(elem).attr('key'), c => c.charCodeAt(0)),
            name: "nathan@nford.xyz",
            displayName: "User",
        },
        pubKeyCredParams: [{ alg: -7, type: "public-key" }],
        authenticatorSelection: {
            authenticatorAttachment: "cross-platform",
        },
        timeout: 60000,
        attestation: "direct"
    };

    const credential = navigator.credentials.create({
        publicKey: publicKeyCredentialCreationOptions
    });
}

customElements.define('l2g-securitykey-link', SecurityKey);