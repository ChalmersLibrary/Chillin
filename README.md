# Chillin

This is the system that is used on Chalmers Library for handling incoming inter library loans and purchases. It is highly tailored to our needs and you should not expect to be able to just take it and use it for your purposes without much hard work and many modifications. Hopefully the code can still be an interesting platform to start from or to draw inspiration from if developing something similar. We welcome constructive criticism and ideas.

## Prerequisites
1. npm https://www.npmjs.com
2. bower http://bower.io

## Setup
1. Clone this repository.
2. Delete all files in the repository except the .git folder.
3. Download Umbraco 6.1.6 and unzip it into the repository folder. https://our.umbraco.org/contribute/releases/616/
4. Rename the unzipped folder to "Chalmers.ILL".
5. Download the ChillinFoundation Umbraco package https://chalmersuniversity.box.com/s/pf9swnh2vgmf9gpn5lox7zmviyze81zr
6. Setup Umbraco, for example by using Microsoft WebMatrix's "Open as a Web Site" capabilities.
7. Install the ChillinFoundation Umbraco package into Umbraco.
8. Create a member and assign the proper roles.
9. Close down Microsoft Web Matrix or corresponding and do "git checkout -- ." in the repository.


