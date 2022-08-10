using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace FlaUInspect.Core;

public class ExtendedObservableCollection<T> : ObservableCollection<T>
{
    public ExtendedObservableCollection()
    {
    }

    public ExtendedObservableCollection(IEnumerable<T> collection) : base(collection)
    {
    }

    public ExtendedObservableCollection(List<T> list) : base(list)
    {
    }

    public void AddRange(IEnumerable<T> range)
    {
        IList<T> rangeList = range as IList<T> ?? range.ToList();
        if (rangeList.Count == 0)
        {
            return;
        }
        if (rangeList.Count == 1)
        {
            this.Add(rangeList[0]);
            return;
        }
        foreach (T item in rangeList)
        {
            this.Items.Add(item);
        }
        this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void RemoveRange(int index, int count)
    {
        if (count <= 0 || index >= this.Items.Count)
        {
            return;
        }
        if (count == 1)
        {
            this.RemoveAt(index);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            this.Items.RemoveAt(index);
        }
        this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
        this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void RemoveAll(Predicate<T> match)
    {
        bool removedItem = false;
        for (int i = this.Items.Count - 1; i >= 0; i--)
        {
            if (match(this.Items[i]))
            {
                this.Items.RemoveAt(i);
                removedItem = true;
            }
        }
        if (removedItem)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public void Reset(IEnumerable<T> range)
    {
        this.ClearItems();
        AddRange(range);
    }
}