from curses.panel import version
import os
import os
import sys
print ("Creating release")
if len(sys.argv) == 1 :
    print ("Error: please provide the release number")
    print("Usage: python createRelease 1.1.1 \"provide an optional comment\"" )
    exit(1)
comment = "default comment"

if len(sys.argv) == 3 :
    comment = sys.argv[2]
 
version_tag = 'v'+ sys.argv[1] 

command = "git tag -a " + version_tag + " -m " + comment

command +=  " && git push origin " + version_tag
print(command)
#os.system(command)