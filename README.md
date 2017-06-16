Generates MD5 hashes for files.
For a single file:
md5gen.exe PathToFile

To read a text list of paths to files and output MD5s to another file of the format "path tab MD5":
md5gen.exe -list PathToInput PathToOutput

To do the same but only include the MD5s:
md5gen.exe -listbare PathToInput PathToOutput

To suppress messages add: -quiet

Constructed by referencing https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
Improved referencing http://blog.monogram.sk/pokojny/2011/09/25/calculating-hash-while-processing-stream/