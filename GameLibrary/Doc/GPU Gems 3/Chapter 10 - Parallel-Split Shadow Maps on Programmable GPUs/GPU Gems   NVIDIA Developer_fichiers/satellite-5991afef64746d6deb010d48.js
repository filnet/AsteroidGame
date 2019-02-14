_satellite.pushAsyncScript(function(event, target, $variables){
  if(typeof jQuery!=='undefined'){
jQuery('#navbar-collapse a').click(function() {
	var $this = jQuery(this);
  var getVal = $this.text();

  if(getVal=='Join'){_satellite.removeCookie('_dtfmch');}if(!jQuery(this).hasClass("dropdown-toggle")){
     if(getVal=='Log out'){
    ClickOmniTrackNavi(this, 'event10,event11,event13', 'link:nav:header:', getVal);
   }else{ClickOmniTrackNavi(this, 'event10,event11', 'link:nav:header:', getVal);}  
  }

});

  
  
  
jQuery('.footer-boilerplate a').click(function() {
	var $this = jQuery(this);
	var getVal = $this.text();
	ClickOmniTracki(this, 'event10', 'link:footer:', getVal);
});}
});
