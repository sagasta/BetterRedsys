# Contributing to BetterRedsys

Thank you for your interest in contributing to **BetterRedsys**! We welcome contributions, including bug reports, feature suggestions, documentation enhancements, and pull requests.

---

## 🛠️ Development Setup

### Prerequisites
- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0) or higher (the libraries target both .NET 10 and .NET 11). Running the tests on both frameworks also requires the .NET 10 runtime.
- Visual Studio 2022 / 2026, JetBrains Rider, or VS Code with C# Dev Kit.

### Clone and Build
```bash
git clone https://github.com/sagasta/BetterRedsys.git
cd BetterRedsys
dotnet restore BetterRedsys.slnx
dotnet build BetterRedsys.slnx -c Release
```

---

## 🧪 Running Tests

BetterRedsys includes a comprehensive xUnit test suite validating cryptographic diversification, response catalogs, REST operations, and simulator behavior:

```bash
dotnet test tests/BetterRedsys.Tests/BetterRedsys.Tests.csproj -c Release
```

---

## 📐 Coding Guidelines

1. **Native AOT Compatibility**: BetterRedsys is Native AOT-ready. Avoid reflection or runtime type emission. All JSON serialization must use compile-time source-generated `JsonSerializerContext`.
2. **Security & Cryptography**: Keep cryptographic operations constant-time (`CryptographicOperations.FixedTimeEquals`). Do not alter 3DES diversification padding rules without verifying against official Redsys test vectors.
3. **Idiomatic modern .NET & C#**: Use primary constructors, file-scoped namespaces, pattern matching, and target-typed `new()`.
4. **Documentation**: Keep public APIs XML-documented (`<summary>`, `<param>`, `<returns>`).

---

## 🚀 Submitting a Pull Request

1. **Fork the repository** and create your branch from `main`:
   ```bash
   git checkout -b feature/my-new-feature
   ```
2. **Write tests** covering your changes.
3. **Ensure all tests pass** (`dotnet test`).
4. **Commit using Conventional Commits**:
   - `feat: ...` for new features
   - `fix: ...` for bug fixes
   - `docs: ...` for documentation
   - `perf: ...` for performance optimizations
   - `test: ...` for test additions
5. **Open a Pull Request** against `main`. Automated GitHub Actions CI will run your branch and verify compilation and test coverage.
