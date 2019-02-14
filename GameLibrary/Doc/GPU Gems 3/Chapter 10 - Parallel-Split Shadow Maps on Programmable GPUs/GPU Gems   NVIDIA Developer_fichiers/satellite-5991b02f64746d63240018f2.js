_satellite.pushAsyncScript(function(event, target, $variables){
  window.ClickOmniTracki=function(obj,eventVal,prefix,secName){  
  try
   {
      secName = prefix+secName+":"+s.pageName ;
     secName=secName.toLowerCase();
      s.events=eventVal;s.linkTrackEvents=eventVal;
      s.eVar10=s.prop10=secName;
      s.linkTrackVars='events,eVar10,prop10';
      s.tl(obj,'o',secName); 
	    s.linkTrackVars="None"
	    s.linkTrackEvents="None"
	 }catch(error){}}
window.ClickOmniTrackNavi=function(obj,eventVal,prefix,secName){  
  try
   {  
     var secN=secName.lastIndexOf('/')+1;
      secName=secName.substring(secN);
      secName=secName.toLowerCase();
      secName=secName.replace(/\//g,':').replace('#','');
      s.eVar11=s.prop11=secName;
      secName = prefix+secName+":"+s.pageName;
      s.events=eventVal;s.linkTrackEvents=eventVal;
      s.eVar10=s.prop10=secName; 
      s.linkTrackVars='events,eVar10,prop10,eVar11';
      s.tl(obj,'o',secName); 
	    s.linkTrackVars="None"
	    s.linkTrackEvents="None"
	 }catch(error){}}


window.ClickOmniFilter=function(obj,eventVal,prefix,secName,filter){  
  try
   {  
     var secN=secName.lastIndexOf('/')+1;
      secName=secName.substring(secN);
      secName=secName.toLowerCase();
      secName=secName.replace(/\//g,':').replace('#','');
     s.eVar35=filter.toLowerCase().replace(/[".]/g,"").replace(/, /g,"|").trim().replace(/: /g,":");
      secName = prefix+secName+":"+s.pageName;
      s.events=eventVal;s.linkTrackEvents=eventVal;
      s.eVar10=s.prop10=secName; 
      s.linkTrackVars='events,eVar10,prop10,eVar35';
      s.tl(obj,'o',secName); 
	    s.linkTrackVars="None"
	    s.linkTrackEvents="None"
	 }catch(error){}}


});
