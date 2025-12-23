using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_shell.src.Interfaces;

public interface IPathResolver
{
    string? FindExecutableInPath(string command);
}

