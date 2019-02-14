_satellite.pushAsyncScript(function(event, target, $variables){
  if(window.location.pathname=='/'||window.location.pathname=='/embedded/twodaystoademo'||window.location.pathname=='/embedded/jetpack'||window.location.pathname=='/computeworks'||window.location.pathname=='/accelerated-computing-toolkit'){
  (function() {
 var w = window, d = document;
 var s = d.createElement('script');
 s.setAttribute('async', 'true');
 s.setAttribute('type', 'text/javascript');
 s.setAttribute('src', '//c1.rfihub.net/js/tc.min.js');
 var f = d.getElementsByTagName('script')[0];
 f.parentNode.insertBefore(s, f);
 if (typeof w['_rfi'] !== 'function') {
  w['_rfi']=function() {
   w['_rfi'].commands = w['_rfi'].commands || [];
   w['_rfi'].commands.push(arguments);
  };
 }
 _rfi('setArgs', 'ver', '9');
 _rfi('setArgs', 'rb', '29879');
 _rfi('setArgs', 'ca', '20776764');
 _rfi('setArgs', '_o', '29879');
 _rfi('setArgs', '_t', '20776764');
 _rfi('track');
})();
}
});
