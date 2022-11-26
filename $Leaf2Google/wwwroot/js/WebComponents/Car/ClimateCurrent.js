class ClimateCurrent extends HTMLElement {
    static get observedAttributes() {
        return ['current'];
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

        const currentTemperature = $('<i>', {
            id: 'currentTemperature',
            class: 'bi bi-thermometer',
            text: '21°'
        }).appendTo(container);

        container.appendTo(shadow)
    }

    connectedCallback() {
        var current = $(this).attr('current');
        $(this.shadowRoot).find('#currentTemperature').text(current + '°');
    }

    disconnectedCallback() {
        console.log('Custom square element removed from page.');
    }

    adoptedCallback() {
        console.log('Custom square element moved to new page.');
    }

    attributeChangedCallback(name, oldValue, newValue) {
        var current = $(this).attr('current');
        $(this.shadowRoot).find('#currentTemperature').text(current + '°');
    }
}

customElements.define('l2g-climatecurrent', ClimateCurrent);