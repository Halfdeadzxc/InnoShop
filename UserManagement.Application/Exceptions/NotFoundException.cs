using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserManagement.Application.Exceptions
{
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string entity, object key)
            : base($"Entity \"{entity}\" ({key}) was not found.") { }

        public NotFoundException(string message) : base(message) { }
    }
}
