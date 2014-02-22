// TODO: reinstate replaceBackBtn - to include the case where people actually really want the back btn
(function($,window,undefined){
  $( window.document ).bind('mobileinit', function(){
    //some class for css to detect touchscreens
    if($.support.touch){
      $('html').addClass('touch');
    }
    var $query = $.mobile.media('screen and (min-width: 480px)') && ($.mobile.media('(-webkit-max-device-pixel-ratio: 1.2)') || $.mobile.media('(max--moz-device-pixel-ratio: 1.2)'));
    $.support.splitview = ($query || ($.mobile.browser.ie && $(this).width() >= 480)) && $.mobile.ajaxEnabled;
    if ($.support.splitview) {
      $('html').addClass('splitview');
      //on window.ready() execution:
      $(function() {
        $(document).unbind('.toolbar');
        $('.ui-page').die('.toolbar');
        $('div:jqmData(role="panel")').addClass('ui-mobile-viewport ui-panel');
        var firstPageMain=$('div:jqmData(id="main") > div:jqmData(role="page"):first');
        if( !$.mobile.hashListeningEnabled || !$.mobile.path.stripHash( location.hash ) ){
          var $container=$('div:jqmData(id="main")');
          $.mobile.firstPage = firstPageMain;
          $.mobile.changePage(firstPageMain, {transition:'none', changeHash:false, pageContainer:$container});
          $.mobile.activePage=undefined;
        } //no need to trigger a hashchange here cause the other page is handled by core.

        // setup the layout for splitview and jquerymobile will handle first page init
        $(window).trigger('orientationchange');
        setTimeout(function(){
          $.mobile.firstPage = firstPageMain;
        }, 100)
      }); //end window.ready()

//----------------------------------------------------------------------------------
//Main event bindings: click, form submits, hashchange and orientationchange/resize(popover)
//----------------------------------------------------------------------------------
      //existing base tag?
      var $window = $( window ),
          $html = $( 'html' ),
          $head = $( 'head' ),
          $base = $head.children( "base" ),
          //tuck away the original document URL minus any fragment.
          documentUrl = $.mobile.path.parseUrl( location.href ),

          //if the document has an embedded base tag, documentBase is set to its
          //initial value. If a base tag does not exist, then we default to the documentUrl.
          documentBase = $base.length ? $.mobile.path.parseUrl( $.mobile.path.makeUrlAbsolute( $base.attr( "href" ), documentUrl.href ) ) : documentUrl;

      function findClosestLink(ele)
      {
        while (ele){
          if (ele.nodeName.toLowerCase() == "a"){
            break;
          }
          ele = ele.parentNode;
        }
        return ele;
      }

      // The base URL for any given element depends on the page it resides in.
      function getClosestBaseUrl( ele )
      {
        // Find the closest page and extract out its url.
        var url = $( ele ).closest( ".ui-page" ).jqmData( "url" ),
          base = documentBase.hrefNoHash;

        if ( !url || !$.mobile.path.isPath( url ) ) {
          url = base;
        }

        return $.mobile.path.makeUrlAbsolute( url, base);
      }

      //simply set the active page's minimum height to screen height, depending on orientation
      function getScreenHeight(){
        var orientation   = jQuery.event.special.orientationchange.orientation(),
          port      = orientation === "portrait",
          winMin      = port ? 480 : 320,
          screenHeight  = port ? screen.availHeight : screen.availWidth,
          winHeight   = Math.max( winMin, $( window ).height() ),
          pageMin     = Math.min( screenHeight, winHeight );

        return pageMin;
      }

      function newResetActivePageHeight(){
        var page=$( "." + $.mobile.activePageClass );
        page.each(function(){
          if($(this).closest(".panel-popover").length != 1){
            $(this).css("min-height", getScreenHeight());
          }
          else {
            $(this).css("min-height", "100%")
          }
        });
          
      }

      //override _registerInternalEvents to bind to new methods below
      $.mobile._registerInternalEvents = function(){
        //DONE: bind form submit with this plugin
        $("form").live('submit', function(event){
          var $this = $( this );
          if( !$.mobile.ajaxEnabled ||
              $this.is( ":jqmData(ajax='false')" ) ){ return; }

          var type = $this.attr("method"),
              target = $this.attr("target"),
              url = $this.attr( "action" ),
              $currPanel=$this.parents('div:jqmData(role="panel")'),
              $currPanelActivePage=$currPanel.children('div.'+$.mobile.activePageClass);

          // If no action is specified, browsers default to using the
          // URL of the document containing the form. Since we dynamically
          // pull in pages from external documents, the form should submit
          // to the URL for the source document of the page containing
          // the form.
          if ( !url ) {
            // Get the @data-url for the page containing the form.
            url = getClosestBaseUrl( $this );
            if ( url === documentBase.hrefNoHash ) {
              // The url we got back matches the document base,
              // which means the page must be an internal/embedded page,
              // so default to using the actual document url as a browser
              // would.
              url = documentUrl.hrefNoSearch;
            }
          }

          url = $.mobile.path.makeUrlAbsolute(  url, getClosestBaseUrl($this) );

          //external submits use regular HTTP
          if( $.mobile.path.isExternal( url ) || target ) {
            return;
          }

          //temporarily put this here- eventually shud just set it immediately instead of an interim var.
          $.mobile.activePage=$currPanelActivePage;
          // $.mobile.pageContainer=$currPanel;
          $.mobile.changePage(
              url, 
              {
                type:       type && type.length && type.toLowerCase() || "get",
                data:       $this.serialize(),
                transition: $this.jqmData("transition"),
                direction:  $this.jqmData("direction"),
                reloadPage: true,
                pageContainer:$currPanel
              }
          );
          event.preventDefault();
        });

        //add active state on vclick
        $( document ).bind( "vclick", function( event ) {
          var link = findClosestLink( event.target );
          if ( link ) {
            if ( $.mobile.path.parseUrl( link.getAttribute( "href" ) || "#" ).hash !== "#" ) {
              $( link ).closest( ".ui-btn" ).not( ".ui-disabled" ).addClass( $.mobile.activeBtnClass );
              $( "." + $.mobile.activePageClass + " .ui-btn" ).not( link ).blur();
            }
          }
        });

        //DONE: link click event binding for changePage
        //click routing - direct to HTTP or Ajax, accordingly
        $(document).bind( "click", function(event) {
          var link = findClosestLink(event.target);
          if (!link){
            return;
          }

          var $link = $(link),
              //remove active link class if external (then it won't be there if you come back)
              httpCleanup = function(){
                window.setTimeout( function() { removeActiveLinkClass( true ); }, 200 );
              };

          //if there's a data-rel=back attr, go back in history
          if( $link.is( ":jqmData(rel='back')" ) ) {
            window.history.back();
            return false;
          }

          //if ajax is disabled, exit early
          if( !$.mobile.ajaxEnabled ){
            httpCleanup();
            //use default click handling
            return;
          }

          var baseUrl = getClosestBaseUrl( $link ),

              //get href, if defined, otherwise fall to null #
              href = $.mobile.path.makeUrlAbsolute( $link.attr( "href" ) || "#", baseUrl ); 

          // XXX_jblas: Ideally links to application pages should be specified as
          //            an url to the application document with a hash that is either
          //            the site relative path or id to the page. But some of the
          //            internal code that dynamically generates sub-pages for nested
          //            lists and select dialogs, just write a hash in the link they
          //            create. This means the actual URL path is based on whatever
          //            the current value of the base tag is at the time this code
          //            is called. For now we are just assuming that any url with a
          //            hash in it is an application page reference.
          if ( href.search( "#" ) != -1 ) {
            href = href.replace( /[^#]*#/, "" );
            if ( !href ) {
              //link was an empty hash meant purely
              //for interaction, so we ignore it.
              event.preventDefault();
              return;
            } else if ( $.mobile.path.isPath( href ) ) {
              //we have apath so make it the href we want to load.
              href = $.mobile.path.makeUrlAbsolute( href, baseUrl );
            } else {
              //we have a simple id so use the documentUrl as its base.
              href = $.mobile.path.makeUrlAbsolute( "#" + href, documentUrl.hrefNoHash );
            }
          }
          
          // Should we handle this link, or let the browser deal with it?
          var useDefaultUrlHandling = $link.is( "[rel='external']" ) || $link.is( ":jqmData(ajax='false')" ) || $link.is( "[target]" ),
              // Some embedded browsers, like the web view in Phone Gap, allow cross-domain XHR
              // requests if the document doing the request was loaded via the file:// protocol.
              // This is usually to allow the application to "phone home" and fetch app specific
              // data. We normally let the browser handle external/cross-domain urls, but if the
              // allowCrossDomainPages option is true, we will allow cross-domain http/https
              // requests to go through our page loading logic.
              isCrossDomainPageLoad = ( $.mobile.allowCrossDomainPages && documentUrl.protocol === "file:" && href.search( /^https?:/ ) != -1 ),

              //check for protocol or rel and its not an embedded page
              //TODO overlap in logic from isExternal, rel=external check should be
              //     moved into more comprehensive isExternalLink
              isExternal = useDefaultUrlHandling || ( $.mobile.path.isExternal( href ) && !isCrossDomainPageLoad ),

              isRefresh=$link.jqmData('refresh'),
              $targetPanel=$link.jqmData('panel'),
              $targetContainer=$('div:jqmData(id="'+$targetPanel+'")'),
              $targetPanelActivePage=$targetContainer.children('div.'+$.mobile.activePageClass),
              $currPanel=$link.parents('div:jqmData(role="panel")'),
              //not sure we need this. if you want the container of the element that triggered this event, $currPanel 
              $currContainer=$.mobile.pageContainer, 
              $currPanelActivePage=$currPanel.children('div.'+$.mobile.activePageClass),
              url=$.mobile.path.stripHash($link.attr("href")),
              from = null;

          //still need this hack apparently:
          $('.ui-btn.'+$.mobile.activeBtnClass).removeClass($.mobile.activeBtnClass);
          $activeClickedLink = $link.closest( ".ui-btn" ).addClass($.mobile.activeBtnClass);

          if( isExternal ) {
            httpCleanup();
            //use default click handling
            return;
          }

          //use ajax
          var transitionVal = $link.jqmData( "transition" ),
              direction = $link.jqmData("direction"),
              reverseVal = (direction && direction === "reverse") ||
                        // deprecated - remove by 1.0
                        $link.jqmData( "back" ),
              //this may need to be more specific as we use data-rel more
              role = $link.attr( "data-" + $.mobile.ns + "rel" ) || undefined,          
              hash = $currPanel.jqmData('hash');

          //if link refers to an already active panel, stop default action and return
          if ($targetPanelActivePage.attr('data-url') == url || $currPanelActivePage.attr('data-url') == url) {
            if (isRefresh) { //then changePage below because it's a pageRefresh request
              $.mobile.changePage(href, {fromPage:from, transition:'fade', reverse:reverseVal, changeHash:false, pageContainer:$targetContainer, reloadPage:isRefresh});
            }
            else { //else preventDefault and return
              event.preventDefault();
              return;
            }
          }
          //if link refers to a page on another panel, changePage on that panel
          else if ($targetPanel && $targetPanel!=$link.parents('div:jqmData(role="panel")')) {
            var from=$targetPanelActivePage;
            $.mobile.pageContainer=$targetContainer;
            $.mobile.changePage(href, {fromPage:from, transition:transitionVal, reverse:reverseVal, pageContainer:$targetContainer});
          }
          //if link refers to a page inside the same panel, changePage on that panel 
          else {
            var from=$currPanelActivePage;
            $.mobile.pageContainer=$currPanel;
            var hashChange= (hash == 'false' || hash == 'crumbs')? false : true;
            $.mobile.changePage(href, {fromPage:from, transition:transitionVal, reverse:reverseVal, changeHash:hashChange, pageContainer:$currPanel});
            //active page must always point to the active page in main - for history purposes.
            $.mobile.activePage=$('div:jqmData(id="main") > div.'+$.mobile.activePageClass);
          }
          event.preventDefault();
        });

        //prefetch pages when anchors with data-prefetch are encountered
        //TODO: insert pageContainer in here!
        $( ".ui-page" ).live( "pageshow.prefetch", function(){
          var urls = [],
              $thisPageContainer = $(this).parents('div:jqmData(role="panel")');
          $( this ).find( "a:jqmData(prefetch)" ).each(function(){
            var url = $( this ).attr( "href" ),
                panel = $(this).jqmData('panel'),
                container = panel.length? $('div:jqmData(id="'+panel+'")') : $thisPageContainer;
            if ( url && $.inArray( url, urls ) === -1 ) {
              urls.push( url );
              $.mobile.loadPage( url, {pageContainer: container} );
            }
          });
        });

        //DONE: bind hashchange with this plugin
        //hashchanges are defined only for the main panel - other panels should not support hashchanges to avoid ambiguity
        $.mobile._handleHashChange = function( hash ) {
          var to = $.mobile.path.stripHash( hash ),
              transition = $.mobile.urlHistory.stack.length === 0 ? "none" : undefined,
              $mainPanel=$('div:jqmData(id="main")'),
              $mainPanelFirstPage=$mainPanel.children('div:jqmData(role="page"):first'),
              $mainPanelActivePage=$mainPanel.children('div.ui-page-active'),
              $menuPanel=$('div:jqmData(id="menu")'),
              $menuPanelFirstPage=$menuPanel.children('div:jqmData(role="page"):first'),
              $menuPanelActivePage=$menuPanel.children('div.ui-page-active'),
              //FIX: temp var for dialogHashKey
              dialogHashKey = "&ui-state=dialog",

              // default options for the changPage calls made after examining the current state
              // of the page and the hash
              changePageOptions = {
                transition: transition,
                changeHash: false,
                fromHashChange: true,
                pageContainer: $mainPanel
              };

          if( !$.mobile.hashListeningEnabled || $.mobile.urlHistory.ignoreNextHashChange ){
            $.mobile.urlHistory.ignoreNextHashChange = false;
            return;
          }

          // special case for dialogs
          if( $.mobile.urlHistory.stack.length > 1 && to.indexOf( dialogHashKey ) > -1 ) {

            // If current active page is not a dialog skip the dialog and continue
            // in the same direction
            if(!$.mobile.activePage.is( ".ui-dialog" )) {
              //determine if we're heading forward or backward and continue accordingly past
              //the current dialog
              $.mobile.urlHistory.directHashChange({
                currentUrl: to,
                isBack: function() { window.history.back(); },
                isForward: function() { window.history.forward(); }
              });

              // prevent changepage
              return;
            } else {
              // var setTo = function() { to = $.mobile.urlHistory.getActive().pageUrl; };
              // if the current active page is a dialog and we're navigating
              // to a dialog use the dialog objected saved in the stack
              // urlHistory.directHashChange({ currentUrl: to, isBack: setTo, isForward: setTo });
              urlHistory.directHashChange({
                currentUrl: to,

                // regardless of the direction of the history change
                // do the following
                either: function( isBack ) {
                  var active = $.mobile.urlHistory.getActive();

                  to = active.pageUrl;

                  // make sure to set the role, transition and reversal
                  // as most of this is lost by the domCache cleaning
                  $.extend( changePageOptions, {
                    role: active.role,
                    transition:  active.transition,
                    reverse: isBack
                  });
                }
              });
            }
          }

          //if to is defined, load it
          if ( to ){
            to = ( typeof to === "string" && !$.mobile.path.isPath( to ) ) ? ( $.mobile.path.makeUrlAbsolute( '#' + to, documentBase ) ) : to;
            //if this is initial deep-linked page setup, then changePage sidemenu as well
            if (!$('div.ui-page-active').length) {
              $menuPanelFirstPage='#'+$menuPanelFirstPage.attr('id');
              $.mobile.changePage($menuPanelFirstPage, {transition:'none', reverse:true, changeHash:false, fromHashChange:false, pageContainer:$menuPanel});
              $.mobile.activePage=undefined;
            }
            $.mobile.activePage=$mainPanelActivePage.length? $mainPanelActivePage : undefined;
            $.mobile.changePage(to, changePageOptions );
          } else {
          //there's no hash, go to the first page in the main panel.
            $.mobile.activePage=$mainPanelActivePage? $mainPanelActivePage : undefined;
            $.mobile.changePage( $mainPanelFirstPage, changePageOptions ); 
          }
        };

        //hashchange event handler
        $(window).bind( "hashchange", function( e, triggered ) {
          $.mobile._handleHashChange( location.hash );
        });

        //set page min-heights to be device specific
        $( document ).bind( "pageshow.resetPageHeight", newResetActivePageHeight );
        $( window ).bind( "throttledresize.resetPageHeight", newResetActivePageHeight );

      }; //end _registerInternalEvents

      //DONE: bind orientationchange and resize - the popover
      _orientationHandler = function(event){
        var $menu=$('div:jqmData(id="menu")'),
            $main=$('div:jqmData(id="main")'),
            $mainHeader=$main.find('div.'+$.mobile.activePageClass+'> div:jqmData(role="header")'),
            $window=$(window);
        
        function popoverBtn(header) {
          if(!header.children('.popover-btn').length){
            if(header.children('a.ui-btn-left').length){
              header.children('a.ui-btn-left').replaceWith('<a class="popover-btn">Menu</a>');
              header.children('a.popover-btn').addClass('ui-btn-left').buttonMarkup();
            }
            else{
              header.prepend('<a class="popover-btn">Menu</a>');
              header.children('a.popover-btn').addClass('ui-btn-left').buttonMarkup()          
            }
          }
        }

        function replaceBackBtn(header) {
          if($.mobile.urlHistory.stack.length > 1 && !header.children('a:jqmData(rel="back")').length && header.jqmData('backbtn')!=false){ 
            header.prepend("<a href='#' class='ui-btn-left' data-"+ $.mobile.ns +"rel='back' data-"+ $.mobile.ns +"icon='arrow-l'>Back</a>" );
            header.children('a:jqmData(rel="back")').buttonMarkup();
          }
        };

        function popover(){
          $menu.addClass('panel-popover')
               .removeClass('ui-panel-left')
               .css({'width':'25%', 'min-width':'250px', 'display':'', 'overflow-x':'visible'});     
          if(!$menu.children('.popover_triangle').length){ 
            $menu.prepend('<div class="popover_triangle"></div>'); 
          }
          $menu.children('.' + $.activePageClass).css('min-height', '100%');
          $main.removeClass('ui-panel-right')
               .css('width', '');
          popoverBtn($mainHeader);

          $main.undelegate('div:jqmData(role="page")', 'pagebeforeshow.splitview');
          $main.delegate('div:jqmData(role="page")','pagebeforeshow.popover', function(){
            var $thisHeader=$(this).children('div:jqmData(role="header")');
            popoverBtn($thisHeader);
          });
          // TODO: unbind resetActivePageHeight for popover pages

        };

        function splitView(){
          $menu.removeClass('panel-popover')
               .addClass('ui-panel-left')
               .css({'width':'25%', 'min-width':'250px', 'display':''});
          $menu.children('.popover_triangle').remove();
          $main.addClass('ui-panel-right')
               .width(function(){
                 return $(window).width()-$('div:jqmData(id="menu")').width();  
               });
          $mainHeader.children('.popover-btn').remove();
          
          // replaceBackBtn($mainHeader);

          $main.undelegate('div:jqmData(role="page")', 'pagebeforeshow.popover');
          $main.delegate('div:jqmData(role="page")', 'pagebeforeshow.splitview', function(){
            var $thisHeader=$(this).children('div:jqmData(role="header")');
            $thisHeader.children('.popover-btn').remove();
            // replaceBackBtn($thisHeader);
          });

        }

        if(event.orientation){
          if(event.orientation == 'portrait'){
            popover();            
          } 
          else if(event.orientation == 'landscape') {
            splitView();
          } 
        }
        else if($window.width() < 768 && $window.width() > 480){
          popover();
        }
        else if($window.width() > 768){
          splitView();
        }
      };

      $(window).bind('orientationchange', _orientationHandler);
      $(window).bind('throttledresize', _orientationHandler);

      //popover button click handler - from http://www.cagintranet.com/archive/create-an-ipad-like-dropdown-popover/
      $('.popover-btn').live('click', function(e){ 
        e.preventDefault(); 
        $('.panel-popover').fadeToggle('fast'); 
        if ($('.popover-btn').hasClass($.mobile.activeBtnClass)) { 
            $('.popover-btn').removeClass($.mobile.activeBtnClass); 
        } else { 
            $('.popover-btn').addClass($.mobile.activeBtnClass); 
        } 
      });

      $('body').live('click', function(event) { 
        if (!$(event.target).closest('.panel-popover').length && !$(event.target).closest('.popover-btn').length) { 
            $(".panel-popover").stop(true, true).hide(); 
            $('.popover-btn').removeClass($.mobile.activeBtnClass); 
        }; 
      });


//----------------------------------------------------------------------------------
//Other event bindings: scrollview, crumbs, data-context and content height adjustments
//----------------------------------------------------------------------------------

      //DONE: pageshow binding for scrollview - now using IScroll4! hell yeah!
      $('div:jqmData(role="page")').live('pagebeforeshow.scroll', function(event, ui){
        if ($.support.touch && !$.support.touchOverflow) {

          var $page = $(this),
              $scrollArea = $page.find('div:jqmData(role="content")');
              $scrAreaChildren = $scrollArea.children();

          if ($scrAreaChildren.length > 1) {
            $scrAreaChildren = $scrollArea.wrapInner("<div class='scrollable vertical'></div>").children();
          }
          $scrollArea.css({ 'width':'auto',
                            'height':'auto',
                            'overflow':'hidden'});
          //TODO: if too many pages are in the DOM that have iscroll on, this might slow down the browser significantly, 
          //in which case we'll need to destroy() the iscroll as the page hides. 
          $scrollArea.iscroll();
        }
      });

      //data-hash 'crumbs' handler
      //now that data-backbtn is no longer defaulting to true, lets set crumbs to create itself even when backbtn is not available
      $('div:jqmData(role="page")').live('pagebeforeshow.crumbs', function(event, data){
        var $this = $(this);
        if($this.jqmData('hash') == 'crumbs' || $this.parents('div:jqmData(role="panel")').data('hash') == 'crumbs'){
          if($this.jqmData('hash')!=false && $this.find('.ui-crumbs').length < 1){
            var $header=$this.find('div:jqmData(role="header")');
              backBtn = $this.find('a:jqmData(rel="back")');
        
            if(data.prevPage.jqmData('url') == $this.jqmData('url')){  //if it's a page refresh
              var prevCrumb = data.prevPage.find('.ui-crumbs');
              crumbify(backBtn, prevCrumb.attr('href'), prevCrumb.find('.ui-btn-text').html());
            }
            else if($.mobile.urlHistory.stack.length > 0) {
              var text = data.prevPage.find('div:jqmData(role="header") .ui-title').html();
              crumbify(backBtn, '#'+data.prevPage.jqmData('url'), text);
            }
            else if(backBtn.length && $.mobile.urlHistory.stack.length <= 1) {
              backBtn.remove();
            }
          }
        }
          
          function crumbify(button, href, text){
            if(!button.length) {
              $this.find('div:jqmData(role="header")').prepend('<a class="ui-crumbs ui-btn-left" data-icon="arrow-l"></a>');
              button=$header.children('.ui-crumbs').buttonMarkup();
            }
            button.removeAttr('data-rel')
                  .jqmData('direction','reverse')
                  .addClass('ui-crumbs')
                  .attr('href',href);
            button.find('.ui-btn-text').html(text);
          }
      });

      //data-context handler - a page with a link that has a data-context attribute will load that page after this page loads
      //this still needs work - pageTransitionQueue messes everything up.
      $('div:jqmData(role="panel")').live('pagechange.context', function(){
        var $this=$(this),
            $currPanelActivePage = $this.children('.' + $.mobile.activePageClass),
            panelContextSelector = $this.jqmData('context'),
            pageContextSelector = $currPanelActivePage.jqmData('context'),
            contextSelector= pageContextSelector ? pageContextSelector : panelContextSelector;
        //if you pass a hash into data-context, you need to specify panel, url and a boolean value for refresh
        if($.type(contextSelector) === 'object') {
          var $targetContainer=$('div:jqmData(id="'+contextSelector.panel+'")'),
              $targetPanelActivePage=$targetContainer.children('div.'+$.mobile.activePageClass),
              isRefresh = contextSelector.refresh === undefined ? false : contextSelector.refresh;
          if(($targetPanelActivePage.jqmData('url') == contextSelector.url && contextSelector.refresh)||(!contextSelector.refresh && $targetPanelActivePage.jqmData('url') != contextSelector.url)){    
              $.mobile.changePage(contextSelector.url, options={transition:'fade', changeHash:false, pageContainer:$targetContainer, reloadPage:isRefresh});
          }
        }
        else if(contextSelector && $currPanelActivePage.find(contextSelector).length){
          $currPanelActivePage.find(contextSelector).trigger('click');
        }
      });

      //this measures the height of header and footer and sets content to the appropriate height so 
      // that no content is concealed behind header and footer
      $('div:jqmData(role="page")').live('pageshow.contentHeight', function(){
        var $this=$(this),
            $header=$this.children(':jqmData(role="header")'),
            $footer=$this.children(':jqmData(role="footer")'),
            thisHeaderHeight=$header.css('display') == 'none' ? 0 : $header.outerHeight(),
            thisFooterHeight=$footer.css('display') == 'none' ? 0 : $footer.outerHeight();
        // $this.children(':jqmData(role="content")').css({'top':thisHeaderHeight, 'bottom':thisFooterHeight});
      })

      //this allows panels to change their widths upon changepage - useful for pages that need a different width than the ones provided. 
      // $('div:jqmData(role="page")').live('')
    }
    else {
      //removes all panels so the page behaves like a single panel jqm
      $(function(){
        $('div:jqmData(role="panel")').each(function(){
          var $this = $(this);
          $this.replaceWith($this.html());
        })
      });
    }
  });
})(jQuery,window);