$(document).ready(function () {
    window.sr = ScrollReveal({ reset: true, duration: 250 });
    var isActive = true;
    var unread = 0;
    var token = $("body").data("token");
    var channelname = $("body").data("channel");
    var channel = $.connection.channelHub;

    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/g, "");
    };

    function linkify(str) {
        return str.replace(/(<a href=")?((https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)))(">(.*)<\/a>)?/gi, function () {
            return '<a href="' + arguments[2] + '">' + (arguments[7] || arguments[2]) + '</a>'
        });
    }

    function addRow(row) {
        $("#messages tr:last").after(row);
        $("#messages tr:last a").oembed();
        sr.reveal('.reveal');
        $("html, body").scrollTop($(document).height());
        $.playSound("/content/notification.mp3");
    }

    $(window).focus(function (event) {
        isActive = true;
        unread = 0;
        document.title = "RazTalk - " + channelname;
    });
    $(window).blur(function (event) {
        isActive = false;
    });

    channel.client.send = function (user, message, timestamp) {
        row = "<tr class=\"reveal\"><td>" + user + "</td><td data-timestamp=\"" + timestamp + "\"><pre>" + linkify(message) + "</pre></td></tr>";
        addRow(row);
        if (!isActive) {
            unread += 1;
            document.title = "(+" + unread + ") RazTalk - " + channelname;
        }
    };
    channel.client.sendInfo = function (info, timestamp) {
        row = "<tr class=\"info reveal\"><td></td><td data-timestamp=\"" + timestamp + "\">" + info + "</td></tr>";
        addRow(row);
    };
    channel.client.updateUsers = function (users) {
        $("#users").text("Connected users: " + users);
    }

    $.connection.hub.start().done(function () {
        if (channel.server.login(token) == false) {
            $("#message").prop("disabled", true);
            channel.client.sendInfo("Connection error");
        }

        $("#message").keypress(function (e) {
            if (e.which == 13 && !e.shiftKey) {
                e.preventDefault();
                msg = $(this);
                if (msg.val().length > 0) {
                    channel.server.send(msg.val().trim());
                }
                msg.val("");
                msg.focus();
            }
        });

        $("#message").prop("disabled", false);
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
