var menuSection;

function includeHtml(basename) {
	$('#include_' + basename).load('includes/' + basename + '.html', function(){ 
		$(this).contents().unwrap(); 
		// select current page menu item
		var index = document.location.href.lastIndexOf("/") + 1;
		var pageName = document.location.href.substr(index);
		if (pageName.indexOf("#") > 0) pageName = pageName.substr(0, pageName.indexOf("#"));
		menuSection = $('a[href*="' + pageName + '"]').parent();
		menuSection.addClass('active');
		menuSection.parent().parent().addClass('active');
		menuSection.children('ul>li>a[href*="' + pageName + '"]').addClass('active');
		menuSection.children('ul:first').css('display', 'block');
		menuSection.parent().parent().children('ul').css('display', 'block');
		menuSection.children('ul>li>a[href*="' + pageName + '"]').next().children('ul').css('display', 'block');
		updateSectionMenu();
	});
}

function updateSectionMenu()
{
    var cur_pos = $(window).scrollTop();
    $('#sidemenu').css('margin-top', (cur_pos+10)+'px');
	var sections = $('section');
  	sections.each(function() {
        var doc_height = $(window).height();
		var nav = menuSection.find('nav'),
			nav_height = doc_height / 3.5; //nav.outerHeight();
	
	    var top = $(this).offset().top - nav_height,
	        bottom = top + $(this).outerHeight();
	    if (cur_pos >= top && cur_pos <= bottom) {
	      nav.find('a').removeClass('active');
	      nav.find('a[href="#'+$(this).attr('id')+'"]').addClass('active');
	    }
    
	});	
}


$( document ).ready(function(){
    
    $('#filler').css('height', ($(window).height() / 2.5) + 'px')
	
	$(window).on('scroll', function () {
	  updateSectionMenu();
	});	

    includeHtml('header');
    includeHtml('sidemenu');
	
});   