# coding: utf-8
import json
from ioHelper import *
def Process(data) -> int:
    moduleVar = IoHelper(data, INIT_MODULE_VAR)
    globalVar = IoHelper(data, INIT_GLOBAL_VAR)
    localVar = IoHelper(data, INIT_LOCAL_VAR)
    moduleVar.JsonText = json.dumps({'ok': True})
    return 0
