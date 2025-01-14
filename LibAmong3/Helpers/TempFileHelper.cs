using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibAmong3.Helpers
{
    public class TempFileHelper : IDisposable
    {
        private readonly Lazy<(string TempDir, Action Cleanup)> _tempDirLazy = new Lazy<(string, Action)>(
            () =>
            {
                var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);
                return (
                    path,
                    () => System.IO.Directory.Delete(path, true)
                );
            }
        );

        private int _fileNum = 0;

        public string GetTempFile(string suffix)
        {
            var path = Path.Combine(
                _tempDirLazy.Value.TempDir,
                $"{++_fileNum}_{suffix}"
            );
            return path;
        }

        public void Dispose()
        {
            if (_tempDirLazy.IsValueCreated)
            {
                try
                {
                    _tempDirLazy.Value.Cleanup();
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
