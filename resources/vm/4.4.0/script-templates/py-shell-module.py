# coding: utf-8
from ioHelper import *


def Process(data) -> int:
    # Do not remove these initializers.
    moduleVar = IoHelper(data, INIT_MODULE_VAR)
    globalVar = IoHelper(data, INIT_GLOBAL_VAR)
    localVar = IoHelper(data, INIT_LOCAL_VAR)

    try:
        # Inputs are read from moduleVar.<name>.
        # Outputs are assigned to moduleVar.<name>.
        # Global/local variables use GetValue(name) and SetValue(name, value).
        pass
    except BaseException as error:
        PrintMsg(error)
        return -1
    return 0
