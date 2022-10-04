$('table').on('click', '#deleteAuth', async function () {
    var data = new FormData();
    data.append('authId', $(this).attr('data-itemid'));

    let action = await fetch(api.Auth.Delete, {
        method: 'POST',
        body: data,
        headers: {
            'Accept': 'application/json'
        }
    });

    action = await action.text();
    $('#sessionWrapper').html(action);

    var clientId = Math.floor(Math.random() * 100);
    var toastdata = new FormData();
    toastdata.append('Title', 'Google Authentication');
    toastdata.append('Message', "The selected authentication has been revoked from Google.");
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
});