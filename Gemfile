source 'https://rubygems.org'

require 'json'
require 'open-uri'
versions = JSON.parse(open('https://pages.github.com/versions.json').read)

gem 'jekyll-gist'
gem 'redcarpet', '~> 3.3.2'
gem 'github-pages', versions['github-pages']
