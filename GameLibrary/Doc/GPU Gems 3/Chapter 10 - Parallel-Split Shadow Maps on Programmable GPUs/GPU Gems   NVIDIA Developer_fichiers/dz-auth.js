/*
 * This client library is used to interact with the NVIDIA Developer authentication.
 */

var clientApp;
var dzAuth0WebAuth;
var dzAuthSessionExist = false;
var dzAuth0SdkVersion = '9.5.1';

function initDzAuth(app) {
    clientApp = app;
    clientApp.settings.custom_domain = clientApp.settings.custom_domain || clientApp.settings.domain;
    clientApp.settings.auto_login = clientApp.settings.auto_login || false;
}

function showDzAuth(state) {
    var destination = window.location.pathname + window.location.search;
    var query_params = {
        method: state,
        state: state,
        redirect_uri: clientApp.settings.redirect_uri + '?destination=' + destination.replace(/^\/+/g, '')
    };
    if (state == 'register') {
        query_params.connection = 'NVIDIA';
    }
    if (state == 'login' && dzAuthSessionExist) {
        query_params.prompt = 'none';
    }
    window.location = buildDzAuthURL(query_params);
}

function buildDzAuthURL(query_params) {
    if(typeof query_params != 'object') {
        return;
    }
    query_params.client_id = clientApp.settings.client_id;
    query_params.scope =  'openid profile email';
    query_params.state = query_params.state || 'login';
    query_params.audience = query_params.audience || 'https://' + clientApp.settings.domain + '/userinfo';
    var query = Object.keys(query_params)
      .map(function(k){return encodeURIComponent(k) + '=' + encodeURIComponent(query_params[k]);})
      .join('&');
    return 'https://' + clientApp.settings.custom_domain + '/authorize?response_type=code&' + query;
}

function dzCheckSession(auth0) {
    dzAuth0WebAuth = dzAuth0WebAuth || new auth0.WebAuth({
          domain: clientApp.settings.custom_domain,
          clientID: clientApp.settings.client_id,
          redirectUri: clientApp.settings.redirect_uri,
          responseType: 'token'
      });

    dzAuth0WebAuth.checkSession({timeout: 5000}, function (err, result) {
        if (!err && result.accessToken) {
            dzAuthSessionExist = true;

            if (clientApp.settings.auto_login) {
                showDzAuth('login');
            }
        }
    });
}

// establish an ajax helper object
var dzajax = {};
// establish the xhr
dzajax.x = function () {
    if (typeof XMLHttpRequest !== 'undefined') {
        return new XMLHttpRequest();
    }
    // technically, this should support all the way down to IE5
    var versions = [
        "MSXML2.XmlHttp.6.0",
        "MSXML2.XmlHttp.5.0",
        "MSXML2.XmlHttp.4.0",
        "MSXML2.XmlHttp.3.0",
        "MSXML2.XmlHttp.2.0",
        "Microsoft.XmlHttp"
    ];

    var xhr;
    for (var i = 0; i < versions.length; i++) {
        try {
            xhr = new ActiveXObject(versions[i]);
            break;
        } catch (e) {
            // do nothing -- try the next version
        }
    }
    return xhr;
};

// the actual send method -- we typically won't call this directly
dzajax.send = function (url, callback, method, data, async) {
    if (async === undefined) {
        async = true;
    }
    var x = dzajax.x();
    x.withCredentials = true;
    x.open(method, url, async);
    x.onreadystatechange = function () {
        if (x.readyState == 4) {
            var json;
            try {
                json = JSON.parse(x.responseText);
            } catch (err) {
                json = {};
            }
            callback(json,x);
        }
    };

    if (method == 'POST') {
        x.setRequestHeader('Content-type', 'application/json; charset=UTF-8');
    }

    x.send(JSON.stringify(data));
};

dzajax.post = function (url, data, callback, async) {
    dzajax.send(url, callback, 'POST', data, async);
};

dzajax.get = function (url, data, callback, async) {
    var query = [];
    for (var key in data) {
        query.push(encodeURIComponent(key) + '=' + encodeURIComponent(data[key]));
    }
    dzajax.send(url + (query.length ? '?' + query.join('&') : ''), callback, 'GET', null, async)
};

function checkSSOLogin() {
    console.log("checkSSOLogin has been deprecated. Please remove it from your code");
    return false;
}
