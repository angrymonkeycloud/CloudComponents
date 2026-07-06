using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.CloudComponents;

public interface IClosable
{
	Task Close();
}