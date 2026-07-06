using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicReaderApp;

public partial class AddBookPage : ContentPage, IQueryAttributable
{
    readonly AddBookViewModel _vm;

    public AddBookPage(AddBookViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("EditBook", out var value) && value is Book book)
        {
            _vm.LoadForEdit(book);
        }
    }
}