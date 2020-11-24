const usersURI = "api/Users"
let users = [];

function GetUsers() {
    fetch(usersURI)
        .then(response => response.json())
        .then(data => _processUsers(data))
        .catch(error => console.error('Unable to get items.', error));
}

function _processUsers(usersData) {

}


const DARK_MODE_COOKIE = "darkmode";

function DarkModeToggle() {
    var bodyClassList = document.body.classList;
    bodyClassList.toggle("dark-mode");
    document.getElementById("lukebot-appbar").classList.toggle("bg-appbar-dark-mode");
    Cookies.set(DARK_MODE_COOKIE, bodyClassList.contains('dark-mode'));
}

$(document).ready(function () {
    var includes = $('[data-include]');
    var promises = [];
    $.each(includes, function (i, obj) {
        var file = 'views/' + $(this).data('include') + '.html';
        promises.push(
            fetch(file)
                .then((response) => response.text())
                .then((html) => {
                    obj.innerHTML = html;
                })
        );
    });

    Promise
        .all(promises)
        .then(() => {
            var darkmode = Cookies.get(DARK_MODE_COOKIE);
            if (darkmode) {
                if (darkmode == 'true') {
                    DarkModeToggle();
                }
            } else {
                Cookies.set(DARK_MODE_COOKIE, 'false');
            }
        });
});
