_satellite.pushAsyncScript(function(event, target, $variables){
  try{
  
  window.ClickDownTrack=function(obj,eventVal,prefix){  
    secName = prefix+":"+s.pageName ;
    secName=secName.toLowerCase();
    s.linkTrackVars='events';
    if(prefix!=='download'){s.eVar4=secName;s.linkTrackVars='events,eVar4';}
    s.events=eventVal;s.linkTrackEvents=eventVal;
     s.tl(obj,'d',secName); 
	  s.linkTrackVars="None"
	  s.linkTrackEvents="None"
}}catch(e){}
setTimeout(function(){
if(typeof jQuery!='undefined'){
jQuery(".col-md-8,col-md-4").on("click","a[href*='driveworks/files/driveinstall-latest']",function(){ClickDownTrack(this,'event4','download');
});
jQuery(".col-md-4").on("click","a[href*='driveworks/files/driveinstall-latest']",function(){ClickDownTrack(this,'event4','download');
});
jQuery("#panelTargetInstaller").on("click","button.cudaDownloadButton", function() {
var getTitle=jQuery(this).parent('a').attr('title');ClickDownTrack(this,'event4',getTitle);
});

jQuery('#agree').on('click', function() {
   var getIframeVal=jQuery(this).parents('#agreement').children('iframe').attr('src').substring(jQuery('#agree').parents('#agreement').children('iframe').attr('src').lastIndexOf('/')+1).replace('.html','');
     ClickDownTrack(this,'event10,event4',getIframeVal)})
if(document.URL.indexOf('embedded/downloads')<0){
jQuery("a[href*=jetpack-l4t]").click(function(){ClickDownTrack(this,'event10','download')});  
}
if(document.URL.indexOf('gameworksdownload')>-1){
jQuery("a[href*=aftermath-12-windows],a[href*=vrworks-24-sdk]").click(function(){ClickDownTrack(this,'event10','download')}); 
}
jQuery('.downloads ul li a.ng-scope.ng-binding').click(function(){
if(jQuery(this).attr('href')!=='#'){
  if(jQuery(this).attr('href').indexOf('index.html')<0){
    if(jQuery(this).attr('href').indexOf('asset')>-1){
      ClickDownTrack(this,'event10','download')
    }
  }
}
})
if(document.URL.indexOf('rdp/cudnn-download')>-1){
jQuery('.panel-body p a').on('click',function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('nvidia-grid-software-management-sdk-downloads')>-1){
  jQuery('#block-system-main a[href*=NVIDIA-GRID-Software-Management-SDK],#block-system-main a[href*=gridsdk-userguide]').click(function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('nvwmi-sdk')>-1){
jQuery('.panel-success a[href*=standalone],.panel-success a[href*=sdk],a[href*=api-reference]').click(function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('user')>-1){
jQuery('#console .container .downloadable-file-link').click(function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('rdp/digits-download')>-1){
  jQuery('a[href*=nv-deep-learning-repo],a[href*=digits-2-ubuntu-1404-prod]').click(function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('video-codec-sdk-archive')>-1){
  jQuery('.col-md-8 a[href*=video-sdk-601],.col-md-8 a[href*=video_codec_sdk]').click(function(){ClickDownTrack(this,'event10','download')})
} 
if(document.URL.indexOf('ffmpeg')>-1){
  jQuery('a[href*=Using_FFmpeg_with_NVIDIA_GPU_Hardware_Acceleration-pdf]').click(function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('designworks/video_codec_sdk/downloads/v8.0')>-1){
jQuery('a[href*=accept_eula]').click(function(){ClickDownTrack(this,'event10','download')})
}
if(document.URL.indexOf('embedded/downloads')>-1){
if(_satellite.getVar('authStage')=='logged-in'){jQuery('.downloads ul li a.ng-scope.ng-binding').click(function(){
if(jQuery(this).attr('href')!=='#'){
  if(jQuery(this).attr('href').indexOf('index.html')<0){
    if(jQuery(this).attr('href').indexOf('embedded')>-1){
      ClickDownTrack(this,'event10','download')
    }
  }
}
}) }else{jQuery('.downloads li a').click(function(){var $this=jQuery(this); var getText=$this.text();ClickOmniTracki(this,'event10','button:section:',getText);})}
}



}},2000);
});
