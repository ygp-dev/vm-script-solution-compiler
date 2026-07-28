# coding: utf-8
import time
from ioHelper import *

def Process(data) -> int:
    moduleVar = IoHelper(data, INIT_MODULE_VAR)
    globalVar = IoHelper(data, INIT_GLOBAL_VAR)
    localVar = IoHelper(data, INIT_LOCAL_VAR)
    try:
        moduleVar.Sum = ((moduleVar.A if moduleVar.A is not None else 3) + (moduleVar.B if moduleVar.B is not None else 4))
        moduleVar.ValuesEcho = (moduleVar.Values if moduleVar.Values is not None else [1, 2, 3])
    except BaseException as error:
        PrintMsg(error)
        return -1
    return 0
