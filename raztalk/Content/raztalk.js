$(document).ready(function () {
    window.sr = ScrollReveal({ reset: true });
    var token = $("body").data("token");
    var channel = $.connection.channelHub;

    function linkify(str) {
        return str.replace(/(<a href=")?((https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)))(">(.*)<\/a>)?/gi, function () {
            return '<a href="' + arguments[2] + '">' + (arguments[7] || arguments[2]) + '</a>'
        });
    }

    function autoscroll() {
        $("html, body").animate({ scrollTop: $(document).height() }, 1000);
    }

    channel.client.send = function (user, message, timestamp) {
        row = "<tr class=\"reveal\"><td>" + user + "</td><td data-timestamp=\"" + timestamp + "\"><pre>" + linkify(message) + "</pre></td></tr>";
        $("#messages tr:last").after(row);
        $("#messages tr:last a").oembed();
        $.playSound("/content/notification.wav");
        sr.reveal('.reveal');
        autoscroll();
    };
    channel.client.sendInfo = function (info) {
        row = "<tr class=\"reveal\"><td></td><td>" + info + "</td></tr>";
        $("#messages tr:last").after(row);
        $("#users").fadeOut("slow", function() { $(this).remove(); });
        $.playSound("/content/notification.wav");
        sr.reveal('.reveal');
        autoscroll();
    };

    $.connection.hub.start().done(function () {
        if (channel.server.login(token) == false) {
            $("#message").prop("disabled", true);
            channel.client.sendInfo("Connection error");
        }

        $("#message").keypress(function (e) {
            if (e.which == 13) {
                e.preventDefault();
                msg = $(this);
                if (msg.val().length > 0) {
                    channel.server.send(msg.val());
                }
                msg.val("");
                msg.focus();
            }
        });
    });
    $.connection.hub.disconnected(function () {
        $("#message").prop("disabled", true);
        channel.client.sendInfo("Disconnected");
    });

    $("pre").each(function () {
        item = $(this);
        item.html(linkify(item.text()));
    });

    $("a").oembed();

    $("#message").focus();

    sr.reveal('.reveal');
});
