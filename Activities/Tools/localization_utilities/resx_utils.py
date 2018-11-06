from lxml import etree as ET
from shutil import copyfile
import os

__XML_NAMESPACE__ = "{http://www.w3.org/XML/1998/namespace}"

class ResourceFileUtil:
    
    def __init__(self, resx_path, new_path=None):
        self.resx_path = resx_path
        if (new_path is not None):
            self.new_path = new_path
        else:
            self.new_path = resx_path 
        parser = ET.XMLParser(remove_blank_text=True)
        self.tree = ET.parse(resx_path, parser)
        self.root = self.tree.getroot()
        self.updated = False
    
    def get_keys(self):
        keys = set()
        data = self.root.findall('./data')
        for item in data:
            keys.add(item.attrib['name'])
        return keys

    def get_resource_value(self, key):
        value = self.root.find("data[@name='{0}']/value".format(key))
        if (value is not None):
            return value.text
        return None

    def get_resource_comment(self, key):
        comment = self.root.find("data[@name='{0}']/comment".format(key))
        if (comment is not None):
            return comment.text
        return None
    
    def get_resource(self, key):
        return self.root.find("data[@name='{0}']".format(key))

    def add_resource(self, key, value = None, comment = None):
        if (self.get_resource_value(key)):
            return False
        new_resource = ET.SubElement(self.root, 'data')
        new_resource.attrib['name'] = key
        new_resource.attrib["{0}space".format(__XML_NAMESPACE__)] = "preserve"
        value_resource = ET.SubElement(new_resource, "value")
        value_resource.text = value
        self.add_comment(new_resource, comment)
        self.updated = True
        return True
    
    def add_comment(self, parent, comment):
        if (parent is not None):
            comment_res = parent.find("./comment")
            if (comment_res is not None and comment is None):
                comment_tail = comment_res.tail
                parent.remove(comment_res)
                parent[0].tail = comment_tail
            elif (comment_res is not None and comment is not None):
                comment_res.text = comment
            elif (comment is not None):
                commet_resource = ET.SubElement(parent, "comment")
                commet_resource.text = comment
                commet_resource.tail = parent[0].tail
                parent[0].tail = parent.text
            self.updated = True
    
    def remove_resources(self, keys):
        data = self.root.findall("./data")
        elements_to_remove = []
        for item in data:
            if (item.attrib['name'] in keys):
                elements_to_remove.append(item)
        for el in elements_to_remove:
            self.root.remove(el)
        if (len(elements_to_remove) > 0):
            self.updated = True

    
    def remove_resource(self, key):
        res = self.root.find("data[@name='{0}']".format(key))
        if (res is not None):
            self.root.remove(res)
        self.updated = True
    
    def update_resource(self, key, value = None, comment = None):
        res = self.root.find("data[@name='{0}']".format(key))
        if (res is None):
            self.add_resource(key, value, comment)
        res_value = res.find("value")
        res_comment = res.find("comment")
        
        if (res_value is None):
            self.remove_resource(key)
            self.add_resource(key, value, comment)
        else:
            res_value.text = value
        
        if (res_comment is None):
            self.add_comment(res, comment)
        else:
            res_comment.text = value
        self.updated = True

    def save(self):
        if (self.updated):
            with open(self.new_path, "w", encoding="utf-8", newline="\r\n") as f:
                f.write(ET.tostring(self.tree, pretty_print=True, encoding="utf-8", method="xml", xml_declaration=True).decode("utf-8"))
            