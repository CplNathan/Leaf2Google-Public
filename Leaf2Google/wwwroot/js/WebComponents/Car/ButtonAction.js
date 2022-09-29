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
            class: 'btn'
        }).appendTo(shadow);
    }

    connectedCallback() {
        var text = $(this).attr('text');
        var colour = $(this).attr('colour');
        var action = $(this).attr('action');

        $(this.shadowRoot).find('#buttonAction').text(text);
        $(this.shadowRoot).find('#buttonAction').addClass('btn-' + colour);

        $(this.shadowRoot).find('#buttonAction').on('click', function () {
            $.ajax({
                url: api.Car.Action + "/?action=" + action + "&duration=" + 5,
                type: "POST",
                data: JSON.stringify({ action: action, duration: 5 }),
                contentType: "application/json",
                cache: false,
                async: false,
                success: function (data) {
                    $('#sessionWrapper').html(data);

                    var clientId = Math.floor(Math.random() * 100);
                    $.ajax({
                        url: api.Toast.Create,
                        type: "POST",
                        data: JSON.stringify({
                            "Title": "Nissan Action",
                            "Message": "The action '" + action + "' has been sent.",
                            "ClientId": clientId,
                            "Colour": "success"
                        }),
                        contentType: "application/json",
                        cache: false,
                        async: false,
                        success: function (data) {
                            $('#toaster').append(data);
                            $('#' + clientId).toast('show');
                        }
                    });
                }
            });
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