# RollingFileWithCleanupAppender

Extension to Log4Net's RollingFileAppender to clean up files on date and file count

## Overview

This RollingFileAppender adds two new parameters to deal with file cleanup, 
MaxNumberOfDays and MaxNumberOfBackups.
The basic premise if this appender is to only allow a certain number of files 
in the logging directory, cleaning up based off of file age and file count.

For the RollingFileAppender, the following parameters are important:

| Parameter     | Description| Example|
| ------------- | -----| ----- |
| File          | Directory and file name| `<file value="..\logs\test.log" /> `  |
| datePattern     | Valid SimpleDateFormatter pattern for rolled file names| `<datePattern value=".yyyyMMdd" />`  |
| preserveLogFileNameExtension |  keeps file extension for rolled files | `<preserveLogFileNameExtension value="true" />`  |
| MaxNumberOfDays | Max age of log file in days to keep | `<maxNumberOfBackups value="5" />`|
| maxNumberOfBackups | Max number files to keep | `<maxNumberOfBackups value="5" />`|


There is an issue that I've encountered with the RollingFileAppender not creating 
the correct Creation Time due to NTF's MaximumTunnelEntryAgeInSeconds.  Because of this, 
the AdjustFileBeforeAppend override closes, touches and re-opens the log file.  I haven't
been able to test for multithread issues in this yet.

See the Notes section on specific issues with file matching on log cleanup.


### MaxNumberOfDays
MaxNumberOfDays indicates the oldest number of days to keep log files.  
Any file in the log directory that matches the File pattern that is older than 
MaxNumberOfDays will be deleted.

### MaxNumberOfBackups
MaxNumberOfBackups indicates the total number of backup logs to keep.
Any file in the log directory that matches the File pattern is evaluated.  
Files are deleted based off of oldest creation time.


### Notes
Due to the difference in file matching (think DIR's * and ?) and RegEx matching 
(think .*), there are significant challenges with figuring out what files "match"
the current DatePattern parameter in the Log4net config.  For example, a typical 
DatePattern could be:
````
<datePattern value=".yyyyMMdd" />

```
However, the pattern used to match files is 
````
if (this.PreserveLogFileNameExtension) {
  return string.Format("{0}.*{1}", Path.GetFileNameWithoutExtension(baseFileName), Path.GetExtension(baseFileName));
} else {
  return string.Format("{0}.*", baseFileName);
}
```
because of the `System.IO.Directory.GetFiles` requirements.  
While possible, generating a full list of files and then using a RegEx to 
match according to the datePattern is significantly hard.

The `System.IO.Directory.GetFiles` call uses a simple file matching pattern 
of `File` + '.\*'  (or `File` + '.\*' + .`Extension` if `preserveLogFileNameExtension` is set)
to match files and may cause files that don't strictly match to be deleted.  A typical 
scenario of this occurring is using Windows Explorer to copy a file.  `Test.log` copied in 
Explorer results in `test - Copy.log` which would get deleted when appropriate
even though it doesn't in any way resemble the `DatePattern`.

Creating a RegEx matching even the 'd' becomes an exercise in futility if the 
`DatePattern` is as simple as '`YYMD`'.

 

