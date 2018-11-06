import sys
import os

def setWorkingFolder(path):
    sys.path.append(path) #adds the path to the module search paths
    os.chdir(path) #changes current dir
    if not hasattr(sys, 'argv'): #this is needed as we don't have argv populated in the activity context and some other module tries to read it
        sys.argv = [path]