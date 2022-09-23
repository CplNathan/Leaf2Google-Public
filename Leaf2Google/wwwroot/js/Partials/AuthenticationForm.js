function onSubmit(token) {
    $('#Captcha').val(token);
    $('form')[0].submit();
}