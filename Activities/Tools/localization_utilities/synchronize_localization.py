from resx_utils import ResourceFileUtil
import os
from pathlib import Path
from os import walk
import re
import sys

project_dir = os.path.join(sys.argv[1], "Properties") #"C:\\work\\Activities\\Cognitive\\UiPath.Cognitive.Activities\\Properties"
resx_name = sys.argv[2] #"UiPath.Cognitive.Activities" 
resx_path = os.path.join(project_dir, "{0}.resx".format(resx_name))
if (not os.path.isfile(resx_path)):
    sys.exit(3)
resx_util = ResourceFileUtil(resx_path)

def sync_resx(original_resx, translated_resx):
    orignin_keys = original_resx.get_keys()
    translated_keys = translated_resx.get_keys()
    keys_to_remove_from_translated = set()
# Get keys in the localized resx that are not in the default one
    for key in translated_keys:
        if key not in orignin_keys:
            keys_to_remove_from_translated.add(key)

# Remove the keys that are not in the localized resx
    translated_resx.remove_resources(keys_to_remove_from_translated)

# Update the keys from the localized resx    
    for key in keys_to_remove_from_translated:
        translated_keys.remove(key)

# Add new keys to the localizaed resx
    for key in orignin_keys:
        original_comm = original_resx.get_resource_comment(key)
        translated_comm = translated_resx.get_resource_comment(key)
        if key not in translated_keys:
            translated_resx.add_resource(key, None, original_comm)
        elif translated_comm != original_comm:
            resource = translated_resx.get_resource(key)
            translated_resx.add_comment(resource, original_comm)
    translated_resx.save()

for (dirpath, dirnames, filenames) in walk(project_dir):
    for filename in filenames:
        filepath = os.path.join(dirpath, filename)
        m = re.search(r"{0}.(\w+).resx".format(resx_name), filepath)
        if (m):
            sync_resx(resx_util, ResourceFileUtil(filepath))

