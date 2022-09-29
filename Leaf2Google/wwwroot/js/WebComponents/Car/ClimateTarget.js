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
            class: 'bi bi-caret-up text-danger'
        }).appendTo(container);

        const targetTemperature = $('<i>', {
            id: 'targetTemperature',
            class: 'bi bi-fan',
            text: '21°'
        }).appendTo(container);

        const decreaseButton = $('<i>', {
            id: 'decreaseButton',
            class: 'bi bi-caret-down text-primary'
        }).appendTo(container);

        container.appendTo(shadow)
    }

    connectedCallback() {
        var target = $(this).attr('target');
        $(this.shadowRoot).find('#targetTemperature').text(target + '°');
    }

    disconnectedCallback() {
        console.log('Custom square element removed from page.');
    }

    adoptedCallback() {
        console.log('Custom square element moved to new page.');
    }

    attributeChangedCallback(name, oldValue, newValue) {
        var target = $(this).attr('target');
        $(this.shadowRoot).find('#targetTemperature').text(target + '°');
    }
}

customElements.define('l2g-climatetarget', ClimateTarget);