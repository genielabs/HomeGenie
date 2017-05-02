var contentTree = {

    main: [
        {
            id: 'about',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Introduction',
                icon: 'info_outline',
                link: '#/about',
                file: 'content/about.md'
            }
        },
        {
            id: 'get_started',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Get started',
                icon: 'build',
                link: '#/get_started',
                file: 'content/install.md'
            }
        },
        {
            id: 'clients',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Clients',
                icon: 'important_devices',
                link: '#/clients',
                file: 'content/clients.md'
            }
        },
        {
            id: 'docs',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Documentation',
                icon: 'import_contacts',
                link: '#/docs/setup'
            },
            list: [
                {
                    id: 'setup',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Setup',
                        link: '#/docs/setup',
                        file: 'content/docs/setup.md'
                    }
                },
                {
                    id: 'configure',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Configuration',
                        link: '#/docs/configure',
                        file: 'content/docs/configure.md'
                    }
                },
                {
                    id: 'scenarios',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Scenarios',
                        link: '#/docs/scenarios',
                        file: 'content/docs/scenarios.md'
                    }
                },
                {
                    id: 'remotes',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'IR/RF remotes',
                        link: '#/docs/remotes',
                        file: 'content/docs/remotes.md'
                    }
                },
                {
                    id: 'scheduling',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Scheduling',
                        link: '#/docs/scheduling',
                        file: 'content/docs/scheduling.md'
                    }
                }/*,
                {
                    id: 'upnp_dlna',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'UPnP/DLNA',
                        link: '#/docs/upnp_dlna',
                        file: 'content/docs/upnp_dlna.md'
                    }
                },
                {
                    id: 'interconnect',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Interconnections',
                        link: '#/docs/interconnect',
                        file: 'content/docs/interconnect.md'
                    }
                }*/
            ]
        },
        {
            id: 'develop',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Developing', // copy some stuff from old site and add second level contents
                icon: 'extension',
                link: '#/develop/programs'
            },
            list: [
                ,
                {
                    id: 'programs',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Programs (APP)',
                        link: '#/develop/programs',
                        file: 'content/devs/programs.md'
                    }
                },
                {
                    id: 'widgets',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Widgets',
                        link: '#/develop/widgets',
                        file: 'content/devs/widgets.md'
                    }
                },
                {
                    id: 'ape',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Program API',
                        link: 'api/ape/annotated.html',
                        attr: 'target="_blank"'
                    }
                },
                {
                    id: 'api',
                    template: 'app/layout/drawer_menu_subitem',
                    data: {
                        title: 'Web API',
                        link: 'api/mig/overview.html',
                        attr: 'target="_blank"'
                    }
                }
            ]
        },
        {
            id: 'source',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Source',
                icon: 'view_headline',
                link: 'https://github.com/genielabs/HomeGenie'
            }
        },
        {
            id: 'community',
            template: 'app/layout/drawer_menu_item',
            data: {
                title: 'Community', // System Integrators
                icon: 'group',
                link: 'https://plus.google.com/communities/116563910167155544886'
            }
        }/*,
        {
             id: 'partners',
             template: 'app/layout/drawer_menu_item',
             data: {
                 title: 'Vendors/S.I.', // System Integrators
                 icon: 'business_center',
                 link: '#'
             }
         },
         {
             id: 'archive',
             template: 'app/layout/drawer_menu_item',
             data: {
                 title: 'Archived',
                 icon: 'archive',
                 link: '#'
             }
         }*/
    ]

};
