using System;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    // ПОДЪЁМ МЕТОДА: интерфейс теперь объявляет общий метод FinishReturn,
    // а не только Issue/Return.
    public interface ICirculationService
    {
        Task<IssueResult> IssueCopyAsync(int copyId, int readerId, int days);
        Task<ReturnResult> ReturnCopyAsync(int copyId);

        Task<ReturnResult> FinishReturnAsync(Loan loan, Copy copy);
    }

    public class CirculationService : ICirculationService
    {
        private readonly ICopyRepository _copyRepository;
        private readonly IReaderRepository _readerRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IValidationService _validationService;

        public CirculationService(
            ICopyRepository copyRepository,
            IReaderRepository readerRepository,
            ILoanRepository loanRepository,
            IValidationService validationService)
        {
            _copyRepository = copyRepository
                              ?? throw new ArgumentNullException(nameof(copyRepository));
            _readerRepository = readerRepository
                                ?? throw new ArgumentNullException(nameof(readerRepository));
            _loanRepository = loanRepository
                              ?? throw new ArgumentNullException(nameof(loanRepository));
            _validationService = validationService
                                 ?? throw new ArgumentNullException(nameof(validationService));
        }

        public async Task<IssueResult> IssueCopyAsync(int copyId, int readerId, int days)
        {
            var (copy, reader) = await LoadCopyAndReaderAsync(copyId, readerId);

            var validationResult = ValidateIssue(copy, reader);
            if (validationResult != IssueResult.Success)
                return validationResult;

            var (issueDate, dueDate) = CalculateIssueDates(days);

            var loan = new Loan
            {
                CopyId = copyId,
                ReaderId = readerId,
                IssueDate = issueDate,
                DueDate = dueDate,
                FineAmount = 0m
            };

            copy!.Status = CopyStatus.OnLoan;

            await SaveLoanAndCopyAsync(loan, copy);

            return IssueResult.Success;
        }

        public async Task<ReturnResult> ReturnCopyAsync(int copyId)
        {
            var copy = await _copyRepository.GetByIdAsync(copyId);
            if (copy is null)
                return ReturnResult.NotFound;

            var loan = await _loanRepository.GetActiveLoanByCopyIdAsync(copyId);
            if (loan is null)
                return ReturnResult.NoActiveLoan;

            // СПУСК МЕТОДА: логика установки даты и состояния займа
            // перенесена в класс Loan (MarkReturned).
            loan.MarkReturned(DateTime.Now, _validationService);

            copy.Status = CopyStatus.Available;

            // ПОДЪЁМ МЕТОДА: общий сценарий завершения возврата поднят
            // в отдельный публичный метод FinishReturnAsync, объявленный в интерфейсе.
            return await FinishReturnAsync(loan, copy);
        }

        public async Task<ReturnResult> FinishReturnAsync(Loan loan, Copy copy)
        {
            await SaveLoanAndCopyAsync(loan, copy);
            return ReturnResult.Success;
        }

        private async Task<(Copy? copy, Reader? reader)> LoadCopyAndReaderAsync(int copyId, int readerId)
        {
            var copyTask = _copyRepository.GetByIdAsync(copyId);
            var readerTask = _readerRepository.GetByIdAsync(readerId);

            await Task.WhenAll(copyTask, readerTask);

            return (copyTask.Result, readerTask.Result);
        }

        private IssueResult ValidateIssue(Copy? copy, Reader? reader)
        {
            if (copy is null || reader is null)
                return IssueResult.NotFound;

            if (!_validationService.CanIssueToReader(reader))
                return IssueResult.ReaderBlocked;

            if (copy.Status != CopyStatus.Available)
                return IssueResult.CopyNotAvailable;

            return IssueResult.Success;
        }

        private static (DateTime issueDate, DateTime dueDate) CalculateIssueDates(int days)
        {
            var now = DateTime.Now;
            return (now, now.AddDays(days));
        }

        private async Task SaveLoanAndCopyAsync(Loan loan, Copy copy)
        {
            await _loanRepository.UpdateOrAddAsync(loan);
            await _copyRepository.UpdateAsync(copy);
        }
    }

    public interface ICopyRepository
    {
        Task<Copy?> GetByIdAsync(int id);
        Task UpdateAsync(Copy copy);
    }

    public interface IReaderRepository
    {
        Task<Reader?> GetByIdAsync(int id);
    }

    public interface ILoanRepository
    {
        Task AddAsync(Loan loan);
        Task UpdateAsync(Loan loan);
        Task<Loan?> GetActiveLoanByCopyIdAsync(int copyId);
        Task UpdateOrAddAsync(Loan loan);
    }

    public interface IValidationService
    {
        bool CanIssueToReader(Reader reader);
        decimal CalculateFine(DateTime dueDate, DateTime returnDate);
    }

    public enum CopyStatus
    {
        Available,
        OnLoan
    }

    public enum IssueResult
    {
        Success,
        NotFound,
        ReaderBlocked,
        CopyNotAvailable
    }

    public enum ReturnResult
    {
        Success,
        NotFound,
        NoActiveLoan
    }

    public class Copy
    {
        public int Id { get; set; }
        public CopyStatus Status { get; set; }
    }

    public class Reader
    {
        public int Id { get; set; }
    }

    public class Loan
    {
        public int Id { get; set; }
        public int CopyId { get; set; }
        public int ReaderId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }

        // ПОДЪЁМ ПОЛЯ: IsOverdue раньше мог быть вычисляемым только в сервисе,
        // теперь это поле/свойство в самой сущности Loan.
        public bool IsOverdue { get; private set; }

        public DateTime? ReturnDate { get; set; }
        public decimal FineAmount { get; set; }

        // СПУСК МЕТОДА: логика установки даты возврата, штрафа и признака просрочки
        // перенесена из CirculationService непосредственно в модель Loan.
        public void MarkReturned(DateTime returnDate, IValidationService validationService)
        {
            ReturnDate = returnDate;
            FineAmount = validationService.CalculateFine(DueDate, returnDate);

            IsOverdue = returnDate.Date > DueDate.Date;
        }
    }
}
