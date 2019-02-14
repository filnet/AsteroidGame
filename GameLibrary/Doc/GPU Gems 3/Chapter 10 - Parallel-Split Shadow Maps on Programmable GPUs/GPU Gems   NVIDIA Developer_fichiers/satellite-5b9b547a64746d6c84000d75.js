_satellite.pushAsyncScript(function(event, target, $variables){
  try{
var cachebuster = Math.round(new Date().getTime() / 1000);
var getV = _satellite.getDataElement('pageName');
var getX;
  if(getV == 'nv:developer:nvidia:embedded-computing'){  
getX = '20805897';
}
  else if (getV == 'nv:developer:nvidia:embedded:buy:jetson-xavier-devkit'){
getX = '20805898';  
}
  else if (getV == 'nv:developer:nvidia:embedded:jetson-xavier-faq'){
getX = '20805899';  
}
  else if (getV == 'nv:developer:nvidia:embedded:develop:hardware'){
getX = '20805900';  
}
  else if (getV == 'nv:developer:nvidia:embedded:develop:software'){
getX = '20805901';  
}
  else if (getV == 'nv:developer:nvidia:embedded:develop:tools'){
getX = '20805902';  
}
  else if (getV == 'nv:developer:nvidia:embedded:downloads'){
getX = '20805903';  
}
  else if (getV == 'nv:developer:nvidia:embedded:community:ecosystem'){
getX = '20805904'; 
}
else if (getV == 'nv:developer:nvidia:embedded:community:support-resources'){
getX = '20805905'; 
}
else if (getV == 'nv:developer:nvidia:embedded:learn:getting-started-jetson'){
getX = '20805906'; 
}
else if (getV == 'nv:developer:nvidia:embedded:buy:jetson-tx2-devkit'){
getX = '20805908'; 
}
else if (getV == 'nv:developer:nvidia:embedded:buy:jetson-tx1-devkit'){
getX = '20805909'; 
}
else if (getV == 'nv:developer:nvidia:embedded:buy:jetson-tx2'){
getX = '20805910'; 
}
else if (getV == 'nv:developer:nvidia:embedded:buy:jetson-tx2i'){
getX = '20805911'; 
}
else if (getV == 'nv:developer:nvidia:embedded:buy:jetson-tx1'){
getX = '20805912'; 
}
else if (getV == 'nv:developer:nvidia:embedded:learn:tutorials'){
getX = '20805913'; 
}
else if (getV == 'nv:developer:nvidia:embedded:faq'){
getX = '20805914'; 
}
else if (getV == 'nv:developer:nvidia:embedded:jetpack'){
getX = '20805915'; 
}
else if (getV == 'nv:developer:nvidia:deepstream-jetson'){
getX = '20805916'; 
}
else if (getV == 'nv:developer:nvidia:isaac-sdk'){
getX = '20805917'; 
}
else if (getV == 'nv:developer:nvidia:smart-cities'){
getX = '20805918'; 
}  
if(getX == '' || typeof getX == 'undefined'){
   }
   else{
   jQuery('body').append('<img src="//'+getX+'p.rfihub.com/ca.gif?rb=29879&ca='+getX+'&_o=29879&_t='+getX+'&ra='+cachebuster+'"height=0 width=0 style="display:none" alt="Rocket Fuel"/>');} 
}catch(e){}
});
