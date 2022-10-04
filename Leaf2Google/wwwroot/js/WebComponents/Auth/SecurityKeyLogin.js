class SecurityKeyLogin extends HTMLElement {
    static get observedAttributes() {
        return [];
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
            class: 'w-100 h-100 shadow'
        });

        const button = $('<button>', {
            id: 'loginKey',
            class: 'btn btn-primary w-100'
        }).appendTo(container);

        const label = $('<i>', {
            id: 'label',
            class: 'bi bi-key-fill'
        }).appendTo(button);

        container.appendTo(shadow)
    }

    connectedCallback() {
        const elem = this;
        const shadow = elem.shadowRoot;

        $(shadow).find('#loginKey').click(function () {
            handleSignInSubmit(elem);
        })

        $(shadow).find('#label').text(' Security Key');
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

async function handleSignInSubmit(event) {
    var data = new FormData();
    data.append('username', '');
    data.append('userVerification', '');

    let makeAssertionOptions;
    try {
        var res = await fetch(api.SecurityKey.MakeAssertionOptions, {
            method: 'POST',
            body: data,
            headers: {
                'Accept': 'application/json'
            }
        });

        makeAssertionOptions = await res.json();
    } catch (e) {
        console.log("Request to server failed", e);
    }

    console.log("Assertion Options Object", makeAssertionOptions);

    if (makeAssertionOptions.status !== "ok") {
        console.log("Error creating assertion options");
        console.log(makeAssertionOptions.errorMessage);
        return;
    }

    const challenge = makeAssertionOptions.challenge.replace(/-/g, "+").replace(/_/g, "/");
    makeAssertionOptions.challenge = Uint8Array.from(atob(challenge), c => c.charCodeAt(0));

    makeAssertionOptions.allowCredentials.forEach(function (listItem) {
        var fixedId = listItem.id.replace(/\_/g, "/").replace(/\-/g, "+");
        listItem.id = Uint8Array.from(atob(fixedId), c => c.charCodeAt(0));
    });

    console.log("Assertion options", makeAssertionOptions);

    let credential;
    try {
        credential = await navigator.credentials.get({ publicKey: makeAssertionOptions })
    } catch (err) {
        console.log(err.message ? err.message : err);
    }

    try {
        await verifyAssertionWithServer(credential);
    } catch (e) {
        console.log("Could not verify assertion", e);
    }
}

async function verifyAssertionWithServer(assertedCredential) {
    let authData = new Uint8Array(assertedCredential.response.authenticatorData);
    let clientDataJSON = new Uint8Array(assertedCredential.response.clientDataJSON);
    let rawId = new Uint8Array(assertedCredential.rawId);
    let sig = new Uint8Array(assertedCredential.response.signature);
    let userHandle = new Uint8Array(assertedCredential.response.userHandle)
    const data = {
        id: assertedCredential.id,
        rawId: coerceToBase64Url(rawId),
        type: assertedCredential.type,
        extensions: assertedCredential.getClientExtensionResults(),
        response: {
            authenticatorData: coerceToBase64Url(authData),
            clientDataJSON: coerceToBase64Url(clientDataJSON),
            userHandle: userHandle !== null ? coerceToBase64Url(userHandle) : null,
            signature: coerceToBase64Url(sig)
        }
    };

    let response;
    try {
        let res = await fetch(api.SecurityKey.MakeAssertion, {
            method: 'POST',
            body: JSON.stringify(data),
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        response = await res.json();
    } catch (e) {
        console.log("Request to server failed", e);
        throw e;
    }

    console.log("Assertion Object", response);

    if (response.status !== "ok") {
        console.log("Error doing assertion");
        console.log(response.errorMessage);
        console.log(response.errorMessage);
        return;
    }

    window.location.reload();
}

coerceToArrayBuffer = function (thing, name) {
    if (typeof thing === "string") {
        thing = thing.replace(/-/g, "+").replace(/_/g, "/");

        var str = window.atob(thing);
        var bytes = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++) {
            bytes[i] = str.charCodeAt(i);
        }
        thing = bytes;
    }

    if (Array.isArray(thing)) {
        thing = new Uint8Array(thing);
    }

    if (thing instanceof Uint8Array) {
        thing = thing.buffer;
    }

    if (!(thing instanceof ArrayBuffer)) {
        throw new TypeError("could not coerce '" + name + "' to ArrayBuffer");
    }

    return thing;
};


coerceToBase64Url = function (thing) {
    if (Array.isArray(thing)) {
        thing = Uint8Array.from(thing);
    }

    if (thing instanceof ArrayBuffer) {
        thing = new Uint8Array(thing);
    }

    if (thing instanceof Uint8Array) {
        var str = "";
        var len = thing.byteLength;

        for (var i = 0; i < len; i++) {
            str += String.fromCharCode(thing[i]);
        }
        thing = window.btoa(str);
    }

    if (typeof thing !== "string") {
        throw new Error("could not coerce to string");
    }

    thing = thing.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "");

    return thing;
};

customElements.define('l2g-securitykey-login', SecurityKeyLogin);