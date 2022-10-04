class ButtonAction extends HTMLElement {
    static get observedAttributes() {
        return ['action', 'colour', 'text'];
    }

    constructor() {
        // Always call super first in constructor
        super();

        const shadow = this.attachShadow({ mode: 'open' });

        const linkBootstrap = $('<link>', {
            rel: 'stylesheet',
            href: '/css/bundle.css'
        }).appendTo(shadow);

        const buttonAction = $('<button>', {
            id: 'buttonAction',
            class: 'btn rounded-pill2'
        }).appendTo(shadow);
    }

    connectedCallback() {
        var text = $(this).attr('text');
        var colour = $(this).attr('colour');
        var command = $(this).attr('action');

        $(this.shadowRoot).find('#buttonAction').text(text);
        $(this.shadowRoot).find('#buttonAction').addClass('btn-' + colour);

        $(this.shadowRoot).find('#buttonAction').on('click', async function () {
            var data = new FormData();
            data.append('action', command);
            data.append('duration', 5);

            let action = await fetch(api.Car.Action, {
                method: 'POST',
                body: data,
                headers: {
                    'Accept': 'application/json'
                }
            });

            action = await action.json();

            var clientId = Math.floor(Math.random() * 100);
            var toastdata = new FormData();
            toastdata.append('Title', 'Nissan Action');
            toastdata.append('Message', "The action '" + action + "' has been sent.");
            toastdata.append('ClientId', clientId);
            toastdata.append('Colour', 'success');

            let toast = await fetch(api.Toast.Create, {
                method: 'POST',
                body: toastdata,
                headers: {
                    'Accept': 'application/json'
                }
            });

            toast = await toast.text();

            $('#toaster').append(toast);
            $('#' + clientId).toast('show');
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

customElements.define('l2g-buttonaction', ButtonAction);