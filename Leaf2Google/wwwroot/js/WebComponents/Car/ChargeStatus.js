class ChargeStatus extends HTMLElement {
    static get observedAttributes() {
        return ['percentage', 'charging'];
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
            id: 'batteryStatus',
            class: 'progress w-100 h-100 shadow',
        });

        const remainingBar = $('<div>', {
            id: 'remainingBar',
            class: 'progress-bar bg-success',
            role: 'progressbar',
            title: 'Vehicle Charge'
        }).appendTo(container);

        const remainingBarIcon = $('<i>', {
            id: 'remainingBarIcon',
            class: 'bi bi-plugin',
            title: 'Charge Remaining'
        }).appendTo(remainingBar)

        const usageBar = $('<div>', {
            id: 'usageBar',
            class: 'progress-bar bg-danger',
            role: 'progressbar',
            title: 'Charge Used'
        }).appendTo(container);

        const optimalBar = $('<div>', {
            id: 'optimalBar',
            class: 'progress-bar bg-warning',
            role: 'progressbar',
            title: 'Optimal Charge'
        }).appendTo(container);

        container.appendTo(shadow)
    }

    connectedCallback() {
        updateStyle(this);
    }

    disconnectedCallback() {
        console.log('Custom square element removed from page.');
    }

    adoptedCallback() {
        console.log('Custom square element moved to new page.');
    }

    attributeChangedCallback(name, oldValue, newValue) {
        updateStyle(this);
    }
}

function updateStyle(elem) {
    const shadow = elem.shadowRoot;

    var percentage = $(elem).attr('percentage');
    $(shadow).find('#remainingBar').width(percentage + "%");
    $(shadow).find('#usageBar').width(100 - percentage - Math.min(100 - percentage, 20) + "%");
    $(shadow).find('#optimalBar').width(Math.min(100 - percentage, 20) + "%");

    $(shadow).find('#remainingBarIcon').text(" " + percentage + "%");

    var charging = $(elem).attr('charging');
    if (charging == "true")
        $(shadow).find('#remainingBar').addClass('progress-bar-striped progress-bar-animated');
    else
        $(shadow).find('#remainingBar').removeClass('progress-bar-striped progress-bar-animated');
}

customElements.define('l2g-chargestatus', ChargeStatus);