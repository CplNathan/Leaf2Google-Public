class ClimateTarget extends HTMLElement {
    static get observedAttributes() {
        return ['target'];
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
            id: 'targetStatus',
            class: 'd-flex flex-column align-items-center justify-content-center h-100 rounded-pill2 bg-white shadow border border-secondary border-2',
            style: 'background-image: linear-gradient(180deg,rgb(255 255 255 / 26%),rgb(92 146 252 / 36%)) !important'
        });

        const increaseButton = $('<i>', {
            id: 'increaseButton',
            class: 'bi bi-caret-up text-danger',
            role: 'button'
        }).appendTo(container);

        const targetTemperature = $('<i>', {
            id: 'targetTemperature',
            class: 'bi bi-fan',
            text: '21°',
            role: 'button'
        }).appendTo(container);

        const decreaseButton = $('<i>', {
            id: 'decreaseButton',
            class: 'bi bi-caret-down text-primary',
            role: 'button'
        }).appendTo(container);

        container.appendTo(shadow)
    }

    connectedCallback() {
        var target = $(this).attr('target');
        var elem = this;
        elem.customtarget = target;

        $(this.shadowRoot).find('#targetTemperature').text(target + '°');

        $(this.shadowRoot).find('#increaseButton').on('click', function () {
            elem.customtarget = parseInt(elem.customtarget) + 1;
            $(elem.shadowRoot).find('#targetTemperature').text(elem.customtarget + '°');
            elem.override = true;
        })

        $(this.shadowRoot).find('#decreaseButton').on('click', function () {
            elem.customtarget = parseInt(elem.customtarget) - 1;
            $(elem.shadowRoot).find('#targetTemperature').text(elem.customtarget + '°');
            elem.override = true;
        })
    }

    disconnectedCallback() {
        console.log('Custom square element removed from page.');
    }

    adoptedCallback() {
        console.log('Custom square element moved to new page.');
    }

    attributeChangedCallback(name, oldValue, newValue) {
        var target = $(this).attr('target');

        if (this?.override && target != this.customtarget) {
            $(this.shadowRoot).find('#targetTemperature').text(this.customtarget + '°');
        }
        else {
            $(this.shadowRoot).find('#targetTemperature').text(target + '°');
            this.override = false;
        }
    }
}

customElements.define('l2g-climatetarget', ClimateTarget);