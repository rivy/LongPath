using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
#if !NET_2_0
using System.Linq;
#endif

namespace Pri.LongPath
{
	using SafeFileHandle = Microsoft.Win32.SafeHandles.SafeFileHandle;
	using FileAccess = System.IO.FileAccess;
	using FileMode = System.IO.FileMode;
	using FileOptions = System.IO.FileOptions;
	using FileShare = System.IO.FileShare;
	using SearchOption = System.IO.SearchOption;

	public static class Directory
	{
		internal static SafeFileHandle GetDirectoryHandle(string normalizedPath)
		{
			var handle = NativeMethods.CreateFile(normalizedPath,
				NativeMethods.EFileAccess.GenericWrite,
				(uint)(FileShare.Write | FileShare.Delete),
				IntPtr.Zero, (int)FileMode.Open, NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
			if (!handle.IsInvalid) return handle;
			var ex = Common.GetExceptionFromLastWin32Error();
			Console.WriteLine("error {0} with {1}\n{2}", ex.Message, normalizedPath, ex.StackTrace);
			throw ex;
		}
#if EXTRAS
		public static void SetAttributes(string path, System.IO.FileAttributes fileAttributes)
		{
			Common.SetAttributes(path, fileAttributes);
		}

		public static System.IO.FileAttributes GetAttributes(string path)
		{
			return Common.GetAttributes(path);
		}
#endif // EXTRAS

		public static string GetCurrentDirectory()
		{
			return Path.RemoveLongPathPrefix(Path.NormalizeLongPath("."));
		}

		public static void Delete(string path, bool recursive)
		{
			try
			{
				foreach (var file in EnumerateFiles(path))
				{
					File.Delete(file);
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				// ignore: not there when we try to delete, it doesn't matter
			}

			try
			{
				foreach (var subPath in EnumerateDirectories(path))
				{
					Delete(subPath, true);
				}
			}
			catch (System.IO.FileNotFoundException)
			{
				// ignore: not there when we try to delete, it doesn't matter
			}

			try
			{
				Delete(path);
			}
			catch (System.IO.FileNotFoundException)
			{
				// ignore: not there when we try to delete, it doesn't matter
			}
		}

		/// <summary>
		///     Deletes the specified empty directory.
		/// </summary>
		/// <param name="path">
		///      A <see cref="String"/> containing the path of the directory to delete.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> could not be found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a directory that is read-only.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a directory that is not empty.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> refers to a directory that is in use.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static void Delete(string path)
		{
			var normalizedPath = Path.NormalizeLongPath(path);

			if (!NativeMethods.RemoveDirectory(normalizedPath))
			{
				throw Common.GetExceptionFromLastWin32Error();
			}
		}

		/// <summary>
		///     Returns a value indicating whether the specified path refers to an existing directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path to check.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="path"/> refers to an existing directory;
		///     otherwise, <see langword="false"/>.
		/// </returns>
		/// <remarks>
		///     Note that this method will return false if any error occurs while trying to determine
		///     if the specified directory exists. This includes situations that would normally result in
		///     thrown exceptions including (but not limited to); passing in a directory name with invalid
		///     or too many characters, an I/O error such as a failing or missing disk, or if the caller
		///     does not have Windows or Code Access Security (CAS) permissions to to read the directory.
		/// </remarks>
		public static bool Exists(string path)
		{
			bool isDirectory;
			return Common.Exists(path, out isDirectory) && isDirectory;
		}

		/// <summary>
		///     Returns a enumerable containing the directory names of the specified directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the directory names within <paramref name="path"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static IEnumerable<string> EnumerateDirectories(string path)
		{
			return EnumerateDirectories(path, null);
		}

		/// <summary>
		///     Returns a enumerable containing the directory names of the specified directory that
		///     match the specified search pattern.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <param name="searchPattern">
		///     A <see cref="String"/> containing search pattern to match against the names of the
		///     directories in <paramref name="path"/>, otherwise, <see langword="null"/> or an empty
		///     string ("") to use the default search pattern, "*".
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the directory names within <paramref name="path"/>
		///     that match <paramref name="searchPattern"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
		{
			return EnumerateFileSystemEntries(path, searchPattern, true, false, System.IO.SearchOption.TopDirectoryOnly);
		}

		public static IEnumerable<string> EnumerateDirectories(string path, string searchPattern, System.IO.SearchOption options)
		{
			return EnumerateFileSystemEntries(path, searchPattern, true, false, options);
		}

		/// <summary>
		///     Returns a enumerable containing the file names of the specified directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the file names within <paramref name="path"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static IEnumerable<string> EnumerateFiles(string path)
		{
			return EnumerateFiles(path, null);
		}

		/// <summary>
		///     Returns a enumerable containing the file names of the specified directory that
		///     match the specified search pattern.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <param name="searchPattern">
		///     A <see cref="String"/> containing search pattern to match against the names of the
		///     files in <paramref name="path"/>, otherwise, <see langword="null"/> or an empty
		///     string ("") to use the default search pattern, "*".
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the file names within <paramref name="path"/>
		///     that match <paramref name="searchPattern"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static IEnumerable<string> EnumerateFiles(string path, string searchPattern)
		{
			return EnumerateFileSystemEntries(path, searchPattern, false, true, System.IO.SearchOption.TopDirectoryOnly);
		}

		public static IEnumerable<string> EnumerateFiles(string path, string searchPattern, System.IO.SearchOption options)
		{
			return EnumerateFileSystemEntries(path, searchPattern, false, true, options);
		}

		/// <summary>
		///     Returns a enumerable containing the file and directory names of the specified directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the file and directory names within
		///     <paramref name="path"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static IEnumerable<string> EnumerateFileSystemEntries(string path)
		{
			return EnumerateFileSystemEntries(path, null);
		}

		/// <summary>
		///     Returns a enumerable containing the file and directory names of the specified directory
		///     that match the specified search pattern.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to search.
		/// </param>
		/// <param name="searchPattern">
		///     A <see cref="String"/> containing search pattern to match against the names of the
		///     files and directories in <paramref name="path"/>, otherwise, <see langword="null"/>
		///     or an empty string ("") to use the default search pattern, "*".
		/// </param>
		/// <returns>
		///     A <see cref="IEnumerable{T}"/> containing the file and directory names within
		///     <paramref name="path"/>that match <paramref name="searchPattern"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
		{
			return EnumerateFileSystemEntries(path, searchPattern, true, true, System.IO.SearchOption.TopDirectoryOnly);
		}

		public static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, System.IO.SearchOption options)
		{
			return EnumerateFileSystemEntries(path, searchPattern, true, true, options);
		}

		private static IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, bool includeDirectories, bool includeFiles, System.IO.SearchOption option)
		{
			var normalizedSearchPattern = Common.NormalizeSearchPattern(searchPattern);
			var normalizedPath = Path.NormalizeLongPath(path);

			return EnumerateNormalizedFileSystemEntries(includeDirectories, includeFiles, option, normalizedPath, normalizedSearchPattern);
		}

		private static IEnumerable<string> EnumerateNormalizedFileSystemEntries(bool includeDirectories, bool includeFiles,
			SearchOption option, string normalizedPath, string normalizedSearchPattern)
		{
			// First check whether the specified path refers to a directory and exists
			System.IO.FileAttributes attributes;
			var errorCode = Common.TryGetDirectoryAttributes(normalizedPath, out attributes);
			if (errorCode != 0)
			{
				throw Common.GetExceptionFromWin32Error(errorCode);
			}

			if (option == System.IO.SearchOption.AllDirectories)
				return EnumerateFileSystemIteratorRecursive(normalizedPath, normalizedSearchPattern, includeDirectories,
					includeFiles);

			return EnumerateFileSystemIterator(normalizedPath, normalizedSearchPattern, includeDirectories, includeFiles);
		}

		private static IEnumerable<string> EnumerateFileSystemIterator(string normalizedPath, string normalizedSearchPattern, bool includeDirectories, bool includeFiles)
		{
			// NOTE: Any exceptions thrown from this method are thrown on a call to IEnumerator<string>.MoveNext()

			var path = Path.RemoveLongPathPrefix(normalizedPath);

			NativeMethods.WIN32_FIND_DATA findData;
			using (var handle = BeginFind(Path.Combine(normalizedPath, normalizedSearchPattern), out findData))
			{
				if (handle == null)
					yield break;

				do
				{
					if (IsDirectory(findData.dwFileAttributes))
					{
						if (includeDirectories && !IsCurrentOrParentDirectory(findData.cFileName))
						{
							yield return Path.Combine(path, findData.cFileName);
						}
					}
					else
					{
						if (includeFiles)
						{
							yield return Path.Combine(path, findData.cFileName);
						}
					}
				} while (NativeMethods.FindNextFile(handle, out findData));

				var errorCode = Marshal.GetLastWin32Error();
				if (errorCode != NativeMethods.ERROR_NO_MORE_FILES)
					throw Common.GetExceptionFromWin32Error(errorCode);
			}
		}

		private static IEnumerable<string> EnumerateFileSystemIteratorRecursive(string normalizedPath, string normalizedSearchPattern, bool includeDirectories, bool includeFiles)
		{
			// NOTE: Any exceptions thrown from this method are thrown on a call to IEnumerator<string>.MoveNext()
			var pendingDirectories = new Queue<string>();
			pendingDirectories.Enqueue(normalizedPath);
			while (pendingDirectories.Count > 0)
			{
				normalizedPath = pendingDirectories.Dequeue();
				// get all subdirs to recurse in the next iteration
				foreach (var subdir in EnumerateNormalizedFileSystemEntries(true, false, System.IO.SearchOption.TopDirectoryOnly, normalizedPath, "*"))
				{
					pendingDirectories.Enqueue(Path.NormalizeLongPath(subdir));
				}

				var path = Path.RemoveLongPathPrefix(normalizedPath);
				NativeMethods.WIN32_FIND_DATA findData;
				using (var handle = BeginFind(Path.Combine(normalizedPath, normalizedSearchPattern), out findData))
				{
					if (handle == null)
						continue;

					do
					{
						var fullPath = Path.Combine(path, findData.cFileName);
						if (IsDirectory(findData.dwFileAttributes))
						{
							var fullNormalizedPath = Path.Combine(normalizedPath, findData.cFileName);
							System.Diagnostics.Debug.Assert(Exists(fullPath));
							System.Diagnostics.Debug.Assert(Exists(Path.RemoveLongPathPrefix(fullNormalizedPath)));
							if (IsCurrentOrParentDirectory(findData.cFileName)) continue;

							if (includeDirectories)
							{
								yield return fullPath;
							}
						}
						else if (includeFiles)
						{
							yield return fullPath;
						}
					} while (NativeMethods.FindNextFile(handle, out findData));

					var errorCode = Marshal.GetLastWin32Error();
					if (errorCode != NativeMethods.ERROR_NO_MORE_FILES)
						throw Common.GetExceptionFromWin32Error(errorCode);
				}
			}
		}

		internal static SafeFindHandle BeginFind(string normalizedPathWithSearchPattern,
			out NativeMethods.WIN32_FIND_DATA findData)
		{
			var handle = NativeMethods.FindFirstFile(normalizedPathWithSearchPattern, out findData);
			if (!handle.IsInvalid) return handle;
			var errorCode = Marshal.GetLastWin32Error();
			if (errorCode != NativeMethods.ERROR_FILE_NOT_FOUND &&
			    errorCode != NativeMethods.ERROR_PATH_NOT_FOUND &&
			    errorCode != NativeMethods.ERROR_NOT_READY)
			{
				throw Common.GetExceptionFromWin32Error(errorCode);
			}

			return null;
		}

		internal static bool IsDirectory(System.IO.FileAttributes attributes)
		{
			return (attributes & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory;
		}

		private static bool IsCurrentOrParentDirectory(string directoryName)
		{
			return directoryName.Equals(".", StringComparison.OrdinalIgnoreCase) || directoryName.Equals("..", StringComparison.OrdinalIgnoreCase);
		}

		public static void Move(string sourcePath, string destinationPath)
		{
			string normalizedSourcePath = Path.NormalizeLongPath(sourcePath, "sourcePath");
			string normalizedDestinationPath = Path.NormalizeLongPath(destinationPath, "destinationPath");

			if (NativeMethods.MoveFile(normalizedSourcePath, normalizedDestinationPath)) return;

			var lastWin32Error = Marshal.GetLastWin32Error();
			if(lastWin32Error == NativeMethods.ERROR_ACCESS_DENIED)
				throw new System.IO.IOException(string.Format("Access to the path '{0}'is denied.", sourcePath), NativeMethods.MakeHRFromErrorCode(lastWin32Error));
			throw Common.GetExceptionFromWin32Error(lastWin32Error, "path");
		}

		/// <summary>
		///     Creates the specified directory.
		/// </summary>
		/// <param name="path">
		///     A <see cref="String"/> containing the path of the directory to create.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     <paramref name="path"/> is an empty string (""), contains only white
		///     space, or contains one or more invalid characters as defined in
		///     <see cref="Path.GetInvalidPathChars()"/>.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> contains one or more components that exceed
		///     the drive-defined maximum length. For example, on Windows-based
		///     platforms, components must not exceed 255 characters.
		/// </exception>
		/// <exception cref="System.IO.PathTooLongException">
		///     <paramref name="path"/> exceeds the system-defined maximum length.
		///     For example, on Windows-based platforms, paths must not exceed
		///     32,000 characters.
		/// </exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">
		///     <paramref name="path"/> contains one or more directories that could not be
		///     found.
		/// </exception>
		/// <exception cref="UnauthorizedAccessException">
		///     The caller does not have the required access permissions.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		///     <paramref name="path"/> is a file.
		///     <para>
		///         -or-
		///     </para>
		///     <paramref name="path"/> specifies a device that is not ready.
		/// </exception>
		/// <remarks>
		///     Note: Unlike <see cref="Directory.CreateDirectory(System.String)"/>, this method only creates
		///     the last directory in <paramref name="path"/>.
		/// </remarks>
		public static DirectoryInfo CreateDirectory(string path)
		{
			var normalizedPath = Path.NormalizeLongPath(path);
			var fullPath = Path.RemoveLongPathPrefix(normalizedPath);

			var length = fullPath.Length;
			if (length >= 2 && Path.IsDirectorySeparator(fullPath[length - 1]))
				--length;

			var rootLength = Path.GetRootLength(fullPath);

			var pathComponents = new List<string>();

			if (length > rootLength)
			{
				for (var index = length - 1; index >= rootLength; --index)
				{
					var subPath = fullPath.Substring(0, index + 1);
					if (!Exists(subPath))
						pathComponents.Add(Path.NormalizeLongPath(subPath));
					while (index > rootLength && fullPath[index] != System.IO.Path.DirectorySeparatorChar &&
						   fullPath[index] != System.IO.Path.AltDirectorySeparatorChar)
						--index;
				}
			}
			while (pathComponents.Count > 0)
			{
				var str = pathComponents[pathComponents.Count - 1];
				pathComponents.RemoveAt(pathComponents.Count - 1);

				if (NativeMethods.CreateDirectory(str, IntPtr.Zero)) continue;

				// To mimic Directory.CreateDirectory, we don't throw if the directory (not a file) already exists
				var errorCode = Marshal.GetLastWin32Error();
				// PR: Not sure this is even possible, we check for existance above.
				//if (errorCode != NativeMethods.ERROR_ALREADY_EXISTS || !Exists(path))
				//{
				throw Common.GetExceptionFromWin32Error(errorCode);
				//}
			}
			return new DirectoryInfo(fullPath);
		}

		public static string[] GetDirectories(string path, string searchPattern, System.IO.SearchOption searchOption)
		{
			return EnumerateDirectories(path, searchPattern, searchOption).ToArray();
		}

		public static string[] GetFiles(string path)
		{
			return EnumerateFiles(path).ToArray();
		}

		public unsafe static void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
		{
			var normalizedPath = Path.NormalizeLongPath(Path.GetFullPath(path));

			using (var handle = GetDirectoryHandle(normalizedPath))
			{
				var fileTime = new NativeMethods.FILE_TIME(creationTimeUtc.ToFileTimeUtc());
				var r = NativeMethods.SetFileTime(handle, &fileTime, null, null);
				if (r) return;
				var errorCode = Marshal.GetLastWin32Error();
				Common.ThrowIOError(errorCode, path);
			}
		}

		public unsafe static void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
		{
			var normalizedPath = Path.NormalizeLongPath(Path.GetFullPath(path));

			using (SafeFileHandle handle = GetDirectoryHandle(normalizedPath))
			{
				var fileTime = new NativeMethods.FILE_TIME(lastWriteTimeUtc.ToFileTimeUtc());
				var r = NativeMethods.SetFileTime(handle, null, null, &fileTime);
				if (r) return;
				var errorCode = Marshal.GetLastWin32Error();
				Common.ThrowIOError(errorCode, path);
			}
		}

		public unsafe static void SetLastAccessTimeUtc(string path, DateTime lastWriteTimeUtc)
		{
			var normalizedPath = Path.NormalizeLongPath(Path.GetFullPath(path));

			using (var handle = GetDirectoryHandle(normalizedPath))
			{
				var fileTime = new NativeMethods.FILE_TIME(lastWriteTimeUtc.ToFileTimeUtc());
				var r = NativeMethods.SetFileTime(handle, null, &fileTime, null);
				if (r) return;
				var errorCode = Marshal.GetLastWin32Error();
				Common.ThrowIOError(errorCode, path);
			}
		}

		public static DirectoryInfo GetParent(string path)
		{
			var directoryName = Path.GetDirectoryName(path);
			return directoryName == null ? null : new DirectoryInfo(directoryName);
		}

		public static DirectoryInfo CreateDirectory(String path, DirectorySecurity directorySecurity)
		{
			CreateDirectory(path);
			SetAccessControl(path, directorySecurity);
			return new DirectoryInfo(path);
		}

		public static DirectorySecurity GetAccessControl(String path)
		{
			const AccessControlSections includeSections = AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group;
			return GetAccessControl(path, includeSections);
		}

		private static void ThrowIfError(int errorCode, IntPtr byteArray)
		{
			if (errorCode == NativeMethods.ERROR_SUCCESS && IntPtr.Zero.Equals(byteArray))
			{
				//
				// This means that the object doesn't have a security descriptor. And thus we throw
				// a specific exception for the caller to catch and handle properly.
				//
				throw new InvalidOperationException("Object does not have security descriptor,");
			}
			if (errorCode == NativeMethods.ERROR_NOT_ALL_ASSIGNED ||
			    errorCode == NativeMethods.ERROR_PRIVILEGE_NOT_HELD)
			{
				throw new PrivilegeNotHeldException("SeSecurityPrivilege");
			}
			if (errorCode == NativeMethods.ERROR_ACCESS_DENIED ||
			    errorCode == NativeMethods.ERROR_CANT_OPEN_ANONYMOUS)
			{
				throw new UnauthorizedAccessException();
			}
			if (errorCode == NativeMethods.ERROR_NOT_ENOUGH_MEMORY)
			{
				throw new OutOfMemoryException();
			}
		}

		public static DirectorySecurity GetAccessControl(String path, AccessControlSections includeSections)
		{
			var normalizedPath = Path.NormalizeLongPath(Path.GetFullPath(path));
			IntPtr sidOwner, sidGroup, dacl, sacl, byteArray;
			var securityInfos = Common.ToSecurityInfos(includeSections);

			var errorCode = (int)NativeMethods.GetSecurityInfoByName(normalizedPath,
				(uint)ResourceType.FileObject,
				(uint)securityInfos,
				out sidOwner,
				out sidGroup,
				out dacl,
				out sacl,
				out byteArray);

			ThrowIfError(errorCode, byteArray);

			var length = NativeMethods.GetSecurityDescriptorLength(byteArray);

			var binaryForm = new byte[length];

			Marshal.Copy(byteArray, binaryForm, 0, (int)length);

			NativeMethods.LocalFree(byteArray);
			var ds = new DirectorySecurity();
			ds.SetSecurityDescriptorBinaryForm(binaryForm);
			return ds;
		}

		public static DateTime GetCreationTime(String path)
		{
			return GetCreationTimeUtc(path).ToLocalTime();
		}

		public static DateTime GetCreationTimeUtc(String path)
		{
			var di = new DirectoryInfo(path);
			return di.CreationTimeUtc;
		}

		public static String[] GetDirectories(String path)
		{
			return EnumerateDirectories(path).ToArray();
		}

		public static String[] GetDirectories(String path, String searchPattern)
		{
			return EnumerateDirectories(path, searchPattern).ToArray();
		}

		public static String GetDirectoryRoot(String path)
		{
			var fullPath = Path.GetFullPath(path);
			return fullPath.Substring(0, Path.GetRootLength(fullPath));
		}

		public static String[] GetFiles(String path, String searchPattern)
		{
			return EnumerateFiles(path, searchPattern).ToArray();
		}

		public static String[] GetFiles(String path, String searchPattern, SearchOption options)
		{
			return EnumerateFiles(path, searchPattern, options).ToArray();
		}

		public static String[] GetFileSystemEntries(String path)
		{
			return EnumerateFileSystemEntries(path).ToArray();
		}

		public static String[] GetFileSystemEntries(String path, String searchPattern)
		{
			return EnumerateFileSystemEntries(path, searchPattern).ToArray();
		}

		public static String[] GetFileSystemEntries(String path, String searchPattern, SearchOption options)
		{
			return EnumerateFileSystemEntries(path, searchPattern, options).ToArray();
		}

		public static DateTime GetLastAccessTime(String path)
		{
			return GetLastAccessTimeUtc(path).ToLocalTime();
		}

		public static DateTime GetLastAccessTimeUtc(String path)
		{
			var di = new DirectoryInfo(path);
			return di.LastAccessTimeUtc;
		}

		public static DateTime GetLastWriteTime(String path)
		{
			return GetLastWriteTimeUtc(path).ToLocalTime();
		}

		public static DateTime GetLastWriteTimeUtc(String path)
		{
			var di = new DirectoryInfo(path);
			return di.LastWriteTimeUtc;
		}

		public static String[] GetLogicalDrives()
		{
			return System.IO.Directory.GetLogicalDrives();
		}

		public static void SetAccessControl(String path, DirectorySecurity directorySecurity)
		{
			if (path == null) throw new ArgumentNullException("path");
			if (directorySecurity == null) throw new ArgumentNullException("directorySecurity");
			var name = Path.NormalizeLongPath(Path.GetFullPath(path));

			Common.SetAccessControlExtracted(directorySecurity, name);
		}

		public static void SetCreationTime(String path, DateTime creationTime)
		{
			SetCreationTimeUtc(path, creationTime.ToUniversalTime());
		}

		public static void SetLastAccessTime(String path, DateTime lastAccessTime)
		{
			SetLastAccessTimeUtc(path, lastAccessTime.ToUniversalTime());
		}

		public static void SetLastWriteTime(String path, DateTime lastWriteTimeUtc)
		{
			unsafe
			{
				var normalizedPath = Path.NormalizeLongPath(Path.GetFullPath(path));

				using (var handle = GetDirectoryHandle(normalizedPath))
				{
					var fileTime = new NativeMethods.FILE_TIME(lastWriteTimeUtc.ToFileTimeUtc());
					var r = NativeMethods.SetFileTime(handle, null, null, &fileTime);
					if (r) return;
					var errorCode = Marshal.GetLastWin32Error();
					Common.ThrowIOError(errorCode, path);
				}
			}
		}

		public static void SetCurrentDirectory(String path)
		{
#if true
			throw new NotSupportedException("Windows does not support setting the current directory to a long path");
#else
			string normalizedPath = Path.NormalizeLongPath(Path.GetFullPath(path));
			if (!NativeMethods.SetCurrentDirectory(normalizedPath))
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (lastWin32Error == NativeMethods.ERROR_FILE_NOT_FOUND)
				{
					lastWin32Error = NativeMethods.ERROR_PATH_NOT_FOUND;
				}
				Common.ThrowIOError(lastWin32Error, normalizedPath);
			}
#endif
		}
	}
}