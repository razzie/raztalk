$(document).ready(function () {
    window.sr = ScrollReveal({ reset: true, duration: 250 });
    var isActive = true;
    var unread = 0;
    var lastMsgUser = "";
    var username = $("body").data("user");
    var channelname = $("body").data("channel");
    var channel = $.connection.channelHub;
    var lastMsgTimestamp = 0;

    String.prototype.trim = function () {
        return this.replace(/^\s+|\s+$/g, "");
    };
    Date.prototype.razformat = function () {
        var year = this.getFullYear();
        var month = this.getMonth() + 1;
        var day = this.getDate();
        var hour = this.getHours();
        var minute = this.getMinutes();
        var second = this.getSeconds();
        return year + "/" + month + "/" + day + " " + hour + ":" + minute + ":" + second;
    }

    function fileExtension(url) {
        return (url = url.substr(1 + url.lastIndexOf("/")).split('?')[0]).split('#')[0].substr(url.lastIndexOf(".")).toLowerCase();
    }
    function isImage(url) {
        var image_types = [".jpg", ".jpeg", ".png", ".gif"];
        var ext = fileExtension(url);
        return (image_types.indexOf(ext) != -1);
    }

    function formatUser(user) {
        if (user == lastMsgUser) {
            return "";
        } else if (user == username) {
            return "<strong>" + user + "</strong>";
        } else {
           return user;
        }
    }
    function errorMsg(err) {
        channel.client.send("", err, lastMsgTimestamp)
    }

    function reconnect() {
        $.connection.hub.start().done(function () {
            channel.client.requestLogin();
        });
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
        if ((timestamp - lastMsgTimestamp) > 600000) {
            lastMsgUser = "";
            var ts = new Date(timestamp)
            var separator = "<tr><td colspan=\"2\" class=\"text-right\"><small>" + ts.razformat() + "</small><hr /></td></tr>";
            $("#messages tr:last").after(separator);
        }

        var row;
        if (user == "") {
            row = "<tr class=\"info reveal\"><td></td><td data-timestamp=\"" + timestamp + "\">" + message + "</td></tr>";
        } else {
            row = "<tr class=\"reveal\"><td>" + formatUser(user) + "</td><td data-timestamp=\"" + timestamp + "\"><pre>" + message + "</pre></td></tr>";
        }

        $("#messages tr:last").after(row);
        msg = $("#messages tr:last pre");

        msg.linkify();
        links = msg.find("a");
        links.oembed();
        links.each(function () {
            var url = $(this).attr("href");
            if (isImage(url)) {
                $(this).html("<img src=\"" + url + "\" />");
            }
        });

        sr.reveal('.reveal');
        $("html, body").scrollTop($(document).height());

        if (!isActive) {
            unread += 1;
            document.title = "(+" + unread + ") RazTalk - " + channelname;
            $.playSound("/content/notification.mp3");
        }

        lastMsgUser = user;
        lastMsgTimestamp = timestamp;
    };
    channel.client.updateUsers = function (users) {
        $("#users").text("Connected users: " + users);
    }
    channel.client.requestLogin = function () {
        body = $("body");
        if (channel.server.login(body.data("user"), body.data("channel"), body.data("pw"), lastMsgTimestamp)) {
            $("#message").prop("disabled", false).focus();
            $(".reconnect").removeAttr("href");
        } else {
            errorMsg("Login failed");
        }
    }

    $.connection.hub.start().done(function () {
        token = $("body").data("token");

        if (channel.server.join(token)) {
            $("#message").prop("disabled", false).focus();
        } else {
            errorMsg("Join failed");
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
    });
    $.connection.hub.disconnected(function () {
        $("#message").prop("disabled", true);
        errorMsg("Disconnected ( <a href=\"#\" class=\"reconnect\" target=\"_self\">Reconnect</a> )");
        $(".reconnect").click(reconnect);
    });

    history.pushState({}, "", "/channel/" + channelname);

    sr.reveal('.reveal');
});
