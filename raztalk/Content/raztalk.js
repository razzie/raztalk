$(document).ready(function () {
    var last_user;
    var token = $("body").data("token");
    var channel = $.connection.channelHub;

    channel.client.send = function (user, message, timestamp) {

    };
    channel.client.sendInfo = function (info) {

    };

    $.connection.hub.start().done(function () {
        channel.server.login(token);
    });

    window.sr = ScrollReveal({ reset: true });
    sr.reveal('.reveal');
});
