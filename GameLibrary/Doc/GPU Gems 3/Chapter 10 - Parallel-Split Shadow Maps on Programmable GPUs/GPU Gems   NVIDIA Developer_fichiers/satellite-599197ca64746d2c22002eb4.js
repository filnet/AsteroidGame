_satellite.pushAsyncScript(function(event, target, $variables){
  try{if(typeof jQuery!=='undefined'){
  jQuery('.footer-links .row .block-content a').click(function(){var $this = jQuery(this);var getVal = $this.text();ClickOmniTracki(this, 'event10', 'link:footer:', getVal);});
  jQuery('.masthead p a.btn').click(function(){var $this = jQuery(this);var getVal = $this.text(); var getHeading= $this.parents('.field').siblings('h1,h2').text(); ClickOmniTracki(this,'event10','button:section:'+getHeading+':',getVal)});
  jQuery('#block-system-main .dev-zone-box .box a').click(function() {
	var $this = jQuery(this);
	var getVal = $this.attr('href').replace('/', '');
	ClickOmniTracki(this, 'event10', 'img:section:', getVal);
});
  if(document.URL.indexOf('/dli/onlinelabs')<0 && document.URL.indexOf('/embedded/community/quick-start-platforms')<0){jQuery('#block-system-main a.btn.btn-primary').click(function() {
	var $this = jQuery(this);
	var getVal = $this.text();
	ClickOmniTracki(this, 'event10', 'link:section:', getVal);
  });}
  if(document.URL.indexOf('/embedded/community/quick-start-platforms')>-1){
  jQuery('#block-system-main .devzone-overlay-hover a.btn').click(function(){var $this= jQuery(this);var getVal = $this.text();var getHeading = $this.siblings('h4').text();ClickOmniTracki(this,'event10','button:section:'+getHeading+':',getVal);});
  }
jQuery('.col-md-4 .row.padding-sm-bottom a img.img-responsive').click(function() {
	var $this = jQuery(this);
	var getVal = $this.parents('a').attr('href').replace('/', '');
	var getHeading = $this.parents('.col-md-4').find('h3').text();
	ClickOmniTracki(this, 'event10', 'img:section:' + getHeading + ':', getVal);
});
jQuery('.col-md-4 .row.padding-sm-bottom .col-xs-8 a').click(function() {
	var $this = jQuery(this);
	var getVal = $this.text();
	var getHeading = $this.parents('.col-md-4').find('h3').text();
	ClickOmniTracki(this, 'event10', 'link:section:' + getHeading + ':', getVal);
});
jQuery('.row .col-md-4.nvidia-md-1-column-margin .field-content img').click(function() {
	var $this = jQuery(this);
	var getVal = $this.parents('a').attr('href').replace('/', '');
	var getHeading = $this.parents('.col-md-4.nvidia-md-1-column-margin').find('h3').text();
	ClickOmniTracki(this, 'event10', 'img:section:' + getHeading + ':', getVal);
});
jQuery('.row .col-md-4.nvidia-md-1-column-margin .title a').click(function() {
	var $this = jQuery(this);
	var getVal = $this.text();
	var getHeading = $this.parents('.col-md-4.nvidia-md-1-column-margin').find('h3').text();
	ClickOmniTracki(this, 'event10', 'link:section:' + getHeading + ':', getVal);
});
jQuery('.col-md-4 .nvidia-lg-absolute-bottom a').click(function() {
	var $this = jQuery(this);
	var getVal = $this.text();
	var getHeading = $this.parents('.col-md-4').find('h3').text();
	ClickOmniTracki(this, 'event10', 'link:section:' + getHeading + ':', getVal);
});}
if(document.URL.indexOf('/embedded-computing')>-1){
jQuery('.jetson-icon-row a i').click(function(){var $this = jQuery(this); var getVal = $this.parents('a').siblings('p').text(); ClickOmniTracki(true,'event10','img:section:',getVal);});
jQuery('.row.isotope a img, .row.isotope a .node-overlay').click(function(){var $this = jQuery(this); var getVal = $this.parents('a.node-thumbnail').siblings('.node-description').find('a').text(); ClickOmniTracki(true,'event10','img:section:',getVal);});
jQuery('.field h4 a').click(function(){var $this = jQuery(this); var getVal = $this.text(); ClickOmniTracki(true,'event10','link:section:',getVal);});
jQuery('.node-description .field-name-title a, .node-description p a').click(function(){var $this = jQuery(this); var getVal = $this.text(); var subHeading = $this.parents('.view-dev-center-blogs').siblings('h4').find('a').text(); var getHeading = $this.parents('p').siblings('.field-name-title').text().substr(0,27); if(getHeading == ''){ClickOmniTracki(true,'event10','link:section:'+subHeading+':',getVal);} else{ClickOmniTracki(true,'event10','link:section:'+subHeading+':'+getHeading+':',getVal);}});
}
if(document.URL.indexOf('/buy-jetson')>-1){
jQuery('.develop-section div a img, a[href="/embedded/buy/jetson-tk1-devkit"] img').click(function(){var $this = jQuery(this); var getVal = $this.attr('alt'); ClickOmniTracki('this','event10','img:section:',getVal);});
jQuery('.develop-section p a, a.btn[href="/embedded/buy/jetson-tk1-devkit"]').click(function(){var $this = jQuery(this); var getVal = $this.text(); var getHeading = $this.parents('p').siblings('h4').text();ClickOmniTracki('this','event10','button:section:'+getHeading+':',getVal);});
jQuery('p em a').click(function(){var $this = jQuery(this); var getVal = $this.text();ClickOmniTracki('this','event10','button:section:',getVal);});
}}catch(e){}
});
