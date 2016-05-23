using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;
using log4net.Appender;
using System.IO;
using log4net.Util;
using System.Text.RegularExpressions;

namespace RollingFileWithCleanupAppender
{
	public class RollingFileWithCleanupAppender : RollingFileAppender
	{
		private readonly static Type declaringType = typeof(RollingFileWithCleanupAppender);

		#region Public Instance Properties

		/// <summary>
		/// Gets or sets the maximum number of backup files that are kept before
		/// the oldest is erased - for DATE rolling.
		/// </summary>
		/// <value>
		/// The maximum number of backup files that are kept before the oldest is
		/// erased - for DATE rolling.
		/// </value>
		/// <remarks>
		/// <para>
		/// If set to zero or negative then all files will be kept.  
		/// </para>
		/// <para>
		/// If a positive number, then that many backups will be kept.  Note that a value of 10 will result in 
		/// 10 backups plus the current file.
		/// </para>
		/// </remarks>		
		public int MaxNumberOfBackups
		{
			get { return m_maxNumberOfBackups; }
			set { m_maxNumberOfBackups = value; }
		}


		/// <summary>
		/// Gets or sets the maximum number of days backup files are kept before
		/// being erased - for DATE rolling.
		/// </summary>
		/// <value>
		/// The maximum number of days backup files are kept before the being
		/// erased - for DATE rolling.
		/// </value>
		/// <remarks>
		/// <para>
		/// If set to zero or negative then all files will be kept.  
		/// </para>
		/// <para>
		/// If a positive number, then any backup file older than days will be erased.
		/// </para>
		/// </remarks>		
		public int MaxNumberOfDays
		{
			get { return m_maxNumberOfDays; }
			set { m_maxNumberOfDays = value; }
		}
		#endregion

		protected override void AdjustFileBeforeAppend()
		{
			LogLog.Debug(declaringType, declaringType.ToString());

			base.AdjustFileBeforeAppend();

			//see if we're 0 sized
			FileInfo fi = new FileInfo(this.File);
			if (fi.Length == 0) {
				//So there's this setting in NTFS - MaximumTunnelEntryAgeInSeconds 
				//That has the potential to cache the create date of a file and reuse a renamed
				//file's original create date - this is just dumb.
				//Because of this, we need to close the file to change the creation time 
				this.CloseFile();
				fi.CreationTime = DateTime.Now;
				LogLog.Debug(declaringType, string.Format("Setting create date to now: [{0}]", fi.CreationTime));

				//And then reopen the file 
				SafeOpenFile(this.File, this.AppendToFile);
			}

			//Now clean out old files
			if (m_maxNumberOfBackups > 0 || m_maxNumberOfDays > 0) {
				LogLog.Debug(declaringType,
											string.Format("Removing older files Max Backups: [{0}] Age limit [{1}] ", m_maxNumberOfBackups, m_maxNumberOfDays));
				RemoveOldLogFiles();
			}
		}

		protected void RemoveOldLogFiles()
		{
			string defaultSearch = string.Empty;
			string defaultMatch = string.Empty;
			try {
				FileInfo fiBase = new FileInfo(base.File);

				//This will search all files that start with the base and end with the extension
				//parsing the datePattern into a RegEx is hard (think Localization...)
				//so we're going to delete anything close.
				//This means that copying a file in explorer in Win7+ requires more thought
				//Test.2016-01-01.log copies to test.2016-01-01 - Copy.log
				defaultSearch = GetWildcardPatternForFile(base.File);

				LogLog.Debug(declaringType, string.Format("Delete files search string: [{0}]", defaultSearch));
				string[] files = Directory.GetFiles(fiBase.DirectoryName, defaultSearch);
				//We need to load this into a list of FileInfos.
				List<FileInfo> fiList = new List<FileInfo>();
				//This will perform badly in large directories - TEST!
				foreach (var file in files) {
					//see if this is old
					fiList.Add(new FileInfo(file));
				}

				//Our delete list
				List<FileInfo> fiToDelete = new List<FileInfo>();


				if (this.m_maxNumberOfDays > 0) {
					//Now, sort by date/time and loop through to add old files 
					foreach (var item in fiList.OrderBy(itm => itm.CreationTime)) {
						if (item.CreationTime < DateTime.Now.AddDays(this.m_maxNumberOfDays * -1)) {
							fiToDelete.Add(item);
						}
					}
				}

				if (this.m_maxNumberOfBackups > 0) {
					int numExtraFiles = (fiList.Count - fiToDelete.Count) - m_maxNumberOfBackups;
					if (numExtraFiles > 0) {
						foreach (var item in fiList.OrderBy(itm => itm.CreationTime)) {
							if (!fiToDelete.Contains(item)) {
								//add this one
								fiToDelete.Add(item);
							}
							if ((fiList.Count - fiToDelete.Count) <= m_maxNumberOfBackups) {
								//we now have enough to delete
								break;
							}

						}
					}
				}

				// Now we can delete
				if (fiToDelete.Count > 0) {
					DeleteFiles(fiToDelete.ToArray());
				}
			} catch (Exception removeEx) {
				ErrorHandler.Error(string.Format("Exception while removing max files [{0}] count [{1}]", defaultSearch, m_maxNumberOfBackups), removeEx, log4net.Core.ErrorCode.GenericFailure);
			}
		}

		protected void DeleteFiles(FileInfo[] files)
		{
			for (int i = 0; i < files.Count(); i++) {
				LogLog.Debug(declaringType, "Deleting file: [" + files[i].FullName + "]");
				System.IO.File.Delete(files[i].FullName);
			}
		}

		/// <summary>
		/// Generates a wildcard pattern that can be used to find all files
		/// that are similar to the base file name.
		/// </summary>
		/// <param name="baseFileName"></param>
		/// <returns></returns>
		private string GetWildcardPatternForFile(string baseFileName)
		{
			//This is a file i/o pattern, not a regex

			if (this.PreserveLogFileNameExtension) {
				return string.Format("{0}.*{1}", Path.GetFileNameWithoutExtension(baseFileName), Path.GetExtension(baseFileName));
			} else {
				return string.Format("{0}.*", baseFileName);
			}
		}

		/// <summary>
		/// Generates a RegEx pattern that can be used to find all files
		/// that are similar to the base file name.
		/// </summary>
		/// <param name="baseFileName"></param>
		/// <returns></returns>
		//private string GetRegexPatternForFile(string baseFileName)
		//{
		//	//This is a regex to match files based off the DatePattern
		//	//It is NOT used because parsing the datePattern is extremely complicated due to the complexity
		//	//of the SimpleDateFormatter and internationalizations

		//	//parse the datePattern
		//	if (this.PreserveLogFileNameExtension) {
		//		return string.Format(@"{0}(.*\.){1}", Path.GetFileNameWithoutExtension(baseFileName), Path.GetExtension(baseFileName).Substring(1));
		//	} else {
		//		return string.Format("{0}(*)", baseFileName);
		//	}
		//}

		//This is a regex to match files based off the DatePattern
		//It is NOT used because parsing the datePattern is extremely complicated due to the complexity
		//of the SimpleDateFormatter and internationalizations
		//private string GetDatePatternRegEx(string datePattern)
		//{
		//	string regex;
		//	StringBuilder pattern = new StringBuilder();

		//	//DatePattern is either a single character or multiple
		//	// if its a single char, most likely the pattern will fail;
		//	//{"d", "D", "f", "F", "g", "G", "m", "o", "r", "s", "t", "T", "u", "U", "Y"};
		//	//Of these, only "D", "M", and "Y" produce valid file names (en-US), but these are highly
		//	//culturally dependent. Return a generic regex
		//	if (this.DatePattern.Length == 1) {
		//		return string.Empty;
		//		//switch (this.DatePattern) {
		//		//	case "D": 
		//		//	case "M":
		//		break;
		//		//	case "Y":
		//		break;
		//		//		pattern.Append(".*");
		//		//		break;
		//		//	default:
		//		//		pattern.Append("");
		//		//		break;
		//		//}
		//	} else {
		//		//Parse this DatePattern into a RegEx
		//		char lastChar = '\x0';
		//		StringBuilder curSegment = new StringBuilder();
		//		foreach (char c in datePattern) {
		//			//same as last
		//			if (c == lastChar) {
		//				curSegment.Append(c);

		//			} else {
		//				//start of a new segment
		//				pattern.Append(ParseSegmentToRegEx(curSegment.ToString()));
		//				//start over
		//				lastChar = c;
		//				curSegment.Clear();
		//			}
		//		}
		//	}
		//	return pattern.ToString();
		//}

		//private string ParseSegmentToRegEx(string segment)
		//{
		//	switch (segment) {
		//		case "d":  //The day of the month, from 1 through 31. 
		//			return "3[01]|[12][0-9]|[1-9]";
		//			break;
		//		case "dd":  //The day of the month, from 01 through 31.
		//			break;
		//		case "ddd": //The abbreviated name of the day of the week.
		//			break;
		//		case "dddd": //The full name of the day of the week.
		//			break;
		//		case "f":  //The tenths of a second in a date and time value.
		//			break;
		//		case "ff": //The hundredths of a second in a date and time value.
		//			break;
		//		case "fff":  //The milliseconds in a date and time value.
		//			break;
		//		case "ffff":  //The ten thousandths of a second in a date and time value.
		//			break;
		//		case "fffff":  //The hundred thousandths of a second in a date and time value.
		//			break;
		//		case "ffffff": //The millionths of a second in a date and time value.
		//			break;
		//		case "fffffff":   //The ten millionths of a second in a date and time value.
		//			break;
		//		case "F":  //If non-zero, the tenths of a second in a date and time value.
		//			break;
		//		case "FF":   //If non-zero, the hundredths of a second in a date and time value.
		//			break;
		//		case "FFF":   //If non-zero, the milliseconds in a date and time value.
		//			break;
		//		case "FFFF":   // If non-zero, the ten thousandths of a second in a date and time value.
		//			break;
		//		case "FFFFF":  //If non-zero, the hundred thousandths of a second in a date and time value.
		//			break;
		//		case "FFFFFF":  //If non-zero, the millionths of a second in a date and time value.
		//			break;
		//		case "FFFFFFF":   /If non-zero, the ten millionths of a second in a date and time value.
		//			break;
		//		case "g":  //The period or era.
		//			break;
		//		case "gg":
		//			break;
		//		case "h":   //The hour, using a 12-hour clock from 1 to 12.
		//			break;
		//		case "hh":  //The hour, using a 12-hour clock from 01 to 12.
		//			break;
		//		case "H":  //The hour, using a 24-hour clock from 0 to 23.
		//			break;
		//		case "HH":   //The hour, using a 24-hour clock from 00 to 23.
		//			break;
		//		case "K":  //Time zone information.
		//			break;
		//		case "m":  //The minute, from 0 through 59.
		//			break;
		//		case "mm":  //The minute, from 00 through 59.
		//			break;
		//		case "M":  //The month, from 1 through 12.
		//			break;
		//		case "MM":  //The month, from 01 through 12.
		//			break;
		//		case "MMM":   //The abbreviated name of the month. 
		//			break;
		//		case "MMMM":  //The full name of the month.
		//			break;
		//		case "s":   //The second, from 0 through 59.
		//			break;
		//		case "ss":   //The second, from 00 through 59.
		//			break;
		//		case "t":   //The first character of the AM/PM designator.
		//			break;
		//		case "tt":  //The AM/PM designator.
		//			break;
		//		case "y":  //The year, from 0 to 99.
		//			break;
		//		case "yy":  //The year, from 00 to 99.
		//			break;
		//		case "yyy":  //The year, with a minimum of three digits.
		//			break;
		//		case "yyyy":  //The year as a four-digit number.
		//			break;
		//		case "yyyyy":  //The year as a five-digit number.
		//			break;
		//		case "z":  //Hours offset from UTC, with no leading zeros.
		//			break;
		//		case "zz":  //Hours offset from UTC, with a leading zero for a single-digit value.
		//			break;
		//		case "zzz":  //Hours and minutes offset from UTC.
		//			break;
		//		default:
		//			break;
		//	}
		//}


		#region Private Instance Fields
		/// <summary>
		/// Default to 0 indicating keep all files
		/// </summary>
		private int m_maxNumberOfBackups = 0;
		/// <summary>
		/// Default to 0 indicating keep all files
		/// </summary>
		private int m_maxNumberOfDays = 0;
		#endregion
	}

}
