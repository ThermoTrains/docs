# Thermo Trains [![Build Status](https://travis-ci.com/sebastianhaeni/thermotrains.svg?token=qgj44e18REfhtFnW25Z2&branch=master)](https://travis-ci.com/sebastianhaeni/thermotrains)

Automatic detection of isolation deficiencies on rolling trains

## Computer Vision Pipeline
<details>
  <summary>Click to expand</summary>
  
  ### Getting started
  
  * Install OpenCV on your machine and add the `bin` directory to your `PATH` variable.
  * To run the Java program, add `-Djava.library.path=lib` as VM option.
  
</details>

## Docs / Slides
<details>
  <summary>Click to expand</summary>
    
  ### Prerequisites

  * [PrinceXML](https://www.princexml.com)
  * [Node.js](https://nodejs.org/en)
    
  ### Getting started

  Execute the following command to install all the dependencies:
   
  `npm install`
  
  **Useful commands:**
  
  * serve docs: `npm run docs`
  * serve slides: `npm run slides`
  * create PDF file: `npm run build`

</details>

## Themobox
<details>
  <summary>Click to expand</summary>
    
  ### Prerequisites
  
  * Visual Studio
  * [FLIR Atlas SDK 5.0](http://flir.custhelp.com/app/devResources/fl_devResources) (needs registration)
    * also install the following from that page:
    * Bonjour: 64-bit
    * Pleora: 64-bit
  * Docker (to easily create a redis instance)
  
  ### Redis
  
  To create a local redis instance, use the following command:
  
  `docker run -p 6379:6379 --name thermobox-redis -d redis`
  
  To connect to it and execute commands, use the following command:
  
  `docker run -it --link thermobox-redis:redis --rm redis redis-cli -h redis -p 6379`
  
  #### Pub commands
  
  * `publish cmd:capture:start <timestamp>` Start capturing
  * `publish cmd:capture:stop <timestamp>` Stop capturing and send the gathered artifacts `upload`
  * `publish cmd:delivery:upload <file>` Uploads the file to a remote server

</details>
