import os.path

validExtensions = ["cs", "fx", "fxh"]

def GetFileExtension(entry):
    index = entry.rfind(".")
    return entry[(index+1):len(entry)]

def GetLinesInDir(indent, dirpath):
    fileCount = 0
    lineCount = 0
    
    contents = os.listdir(dirpath)
    for entry in contents:
        entry = dirpath + "/" + entry
        if (os.path.isfile(entry)):
            if (entry.count(".svn") == 0 and (GetFileExtension(entry)) in validExtensions):
                print indent + entry
                infile = open(entry, "r")
                fileCount += 1
                while infile:
                    if (len(infile.readline()) == 0):
                        break
                    lineCount += 1
        else:
            #print indent + " not a file : " + entry
            if (os.path.isdir(entry) and not entry.count(".svn") > 0):
                nFile, nLine = GetLinesInDir(indent + " ", entry)
                fileCount += nFile
                lineCount += nLine

    answer = {}
    
    return (fileCount, lineCount)


files, lines = GetLinesInDir("", os.curdir)
print str(lines) + " lines in " + str(files) + " classes"
