async function updatePanels() {
    $('l2g-chargestatus').each(async function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        var data = new FormData();
        data.append('query', 'battery');
        data.append('vin', vin);

        let battery = await fetch(api.Car.Status, {
            method: 'POST',
            body: data,
            headers: {
                'Accept': 'application/json'
            }
        });

        battery = await battery.json();

        control.attr('percentage', battery.percentage);
        control.attr('charging', battery.charging);
    });

    $('l2g-locationmap').each(async function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        var data = new FormData();
        data.append('query', 'location');
        data.append('vin', vin);

        let location = await fetch(api.Car.Status, {
            method: 'POST',
            body: data,
            headers: {
                'Accept': 'application/json'
            }
        });

        location = await location.json();

        control.attr('lat', location.lat);
        control.attr('long', location.long);
    })

    $('l2g-climatecurrent').each(async function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        var data = new FormData();
        data.append('query', 'climate');
        data.append('vin', vin);

        let climate = await fetch(api.Car.Status, {
            method: 'POST',
            body: data,
            headers: {
                'Accept': 'application/json'
            }
        });

        climate = await climate.json();

        control.attr('current', climate.current);
    })

    $('l2g-climatetarget').each(async function (index) {
        var control = $(this);
        var vin = control.attr('vin') ?? null;

        var data = new FormData();
        data.append('query', 'climate');
        data.append('vin', vin);

        let climate = await fetch(api.Car.Status, {
            method: 'POST',
            body: data,
            headers: {
                'Accept': 'application/json'
            }
        });

        control.attr('target', climate.target);
    })
}

function checkForInput(element) {
    if ($(element).val().length > 0) {
        $(element).addClass('active');
    } else {
        $(element).removeClass('active');
    }
}

$(document).ready(function () {
    $('.form-control').each(function () {
        checkForInput(this);
    });

    $('.form-control').on('change keyup', function () {
        checkForInput(this);
    });

    $('.toast').toast('show');
    setInterval(updatePanels, 5000);
});