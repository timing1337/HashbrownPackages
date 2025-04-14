using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashbrownPackages.Games
{
    public abstract class BaseGame
    {
        public abstract string GameName { get; }

        public void Prepare()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), GameName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public string GetAssetPath(string fileName)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), GameName, fileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public abstract void Process();

        public abstract void ProcessRawFile();
        public abstract void ProcessAnimPkg();
    }
}
