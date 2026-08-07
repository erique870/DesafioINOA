# DESAFIO BT - INOA - stock-quote-alert

Aplicação de console em C# (.NET 10) que monitora continuamente a cotação de um ativo da B3 e envia um e-mail de alerta quando o preço ultrapassa uma referência de venda ou cai abaixo de uma referência de compra.

## Pré-requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) instalado.
- Uma conta de e-mail com acesso SMTP (Gmail, Outlook, um SMTP corporativo etc).
  - Se utilizar Gmail, é necessário gerar uma **senha de app** em https://myaccount.google.com/apppasswords.

## Estrutura do projeto

```
stock-quote-alert/
├── stock-quote-alert.csproj   # arquivo de projeto .NET
├── Program.cs                 # ponto de entrada / loop de monitoramento
├── AppConfig.cs               # modelos de configuração (config.json)
├── QuoteProvider.cs           # cliente da API de cotações (brapi.dev nesse projeto)
├── EmailAlertSender.cs        # envio de e-mail de alerta via SMTP
├── _config.json               # modelo do arquivo de configuração
└── README.md
```

## Passo a passo

### 1. Restaurar/compilar o projeto

```bash
cd stock-quote-alert
dotnet build
```

Isso baixa o SDK do .NET necessário e compila o projeto. Não há dependências de terceiros (NuGet): o programa usa apenas bibliotecas nativas do .NET (`System.Net.Mail`, `System.Net.Http`, `System.Text.Json`), então não é necessário nenhum `dotnet add package`.

### 2. Criar o arquivo de configuração

Copie o exemplo e edite com os dados corretos:

```bash
cp _config.json config.json
```

Edite `config.json`:

```json
{
  "Email": {
    "To": "seu-email@exemplo.com",
    "From": "alertas@exemplo.com",
    "FromName": "Stock Quote Alert"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "User": "alertas@exemplo.com",
    "AppPassword": "sua-senha-de-app-aqui",
    "EnableSsl": true
  },
  "BrapiToken": "",
  "QuoteUpdateIntervalSeconds": 30
}
```

- `Email.To`: endereço de e-mail que recebe os alertas de cotação.
- `Email.From` / `Smtp.User`: geralmente a mesma conta autenticada no SMTP.
- `Smtp.Host` / `Smtp.Port`: dados do seu provedor de e-mail (Gmail: `smtp.gmail.com` / `587`; Outlook: `smtp.office365.com` / `587`).
- `QuoteUpdateIntervalSeconds`: intervalo de tempo, em segundos, entre cada consulta de cotação.
- `BrapiToken`: token brapi customizado opcional. A API [brapi.dev](https://brapi.dev) funciona sem token no plano gratuito, sujeita a limites de uso.

### 3. Rodar o programa


O programa pode ser rodado via dotnet com o seguinte formato de comando:

```
dotnet run <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>
```

Para rodar o programa via executáveis, faça:

Para Windows:

    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Para Linux:

    dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

O executável será gerado na pasta `bin/Release/net10.0/linux-x64/publish` ou `bin/Release/net10.0/win-x64/publish`. 

Ponha o executável na mesma pasta que o arquivo `config.json` já preenchido e execute:

Para Windows:

    .\stock-quote-alert.exe <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>

Para Linux

    ./stock-quote-alert <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>

O programa então:

1. Valida os argumentos e carrega o arquivo de configuração `config.json` ou retorna erros se houver campos incompletos ou inconsistentes no arquivo.
2. Entra em loop, consultando a cotação do ativo informado a cada `QuoteUpdateIntervalSeconds` segundos.
3. Imprime no console cada cotação lida.
4. Quando o preço fica **acima** do limiar de venda, envia um e-mail de alerta sugerindo a venda do ativo.
5. Quando o preço fica **abaixo** do limiar de compra, envia um e-mail de alerta sugerindo a compra do ativo.
6. Para evitar o envio de vários alertas do mesmo tipo (Compra/Venda) seguidos, cada alerta só é reenviado depois que o preço volta para dentro da faixa entre os limiares e cruza a referência novamente.
7. `Ctrl+C` no terminal de execução encerra o monitoramento do ativo.

## Sobre a API de cotação

O projeto usa a API **[brapi.dev](https://brapi.dev/)**, que cobre ativos da B3 (ex.: `PETR4`, `VALE3`, `ITUB4`). Ela foi escolhida por ser gratuita e não exigir cadastro para uso básico.

## Fluxo de Trabalho (Útil para avaliação)

1. Construí a estrutura do parser com exatamente três argumentos em `Program.cs`. 
    - A string que representa o nome do ativo é ajustada para o padrão adequado com caracteres em caixa alta. 
    - O limiar de venda deve ser estritamente maior que o limiar de compra.

2. Antes de fazer qualquer requisição de cotação, me preocupei em construir a estrutura de envio de alertas por e-mail, que corresponde ao arquivo `EmailAlertSender.cs`. 
    - Utilizei bibliotecas nativas do .NET: `System.Net` e `System.Net.Mail`. 
    - Informações de e-mail (`EmailSettings`) e smtp (`SmtpSettings`) foram alocadas na classe `AppConfig` do arquivo `AppConfig.cs` facilitando a implementação de novos atributos futuramente. 

3. Com a estrutura de envio de alertas funcionais, trabalhei na importação das informações de `EmailSetting` e `SmtpSettings` a partir de um arquivo de configuração solicitado.
    - Escolhi `.json` como formato de arquivo para essa finalidade. Utilizei a biblioteca `System.Text.Json` para fazer a leitura do arquivo e, em seguida, verifiquei se os campos importados são adequados para as suas finalidades, verificando nulidade de atributos e range de porta informado em `Program.cs`.

4. Para as requisições de cotação de ativos, escolhi a API brapi.dev. 
    - Usei as bibliotecas `System.Net.Http.Json` e `System.Text.Json.Serialization` no arquivo `QuoteProvider.cs` para coletar e retornar o atributo `regularMarketPrice`, que será usado em `Program.cs`.

5. Com as estruturas de parsing, envio de alertas e requisição de cotação funcionais:
    - Construí o loop de monitoramento do ativo informado, fazendo requisições à API a cada `QuoteUpdateIntervalSeconds` segundos (informado no arquivo de configuração `config.json`), comparando com os valores de referência de compra e venda e enviando alertas conforme a lógica do desafio solicita.
    - Escrevi o corpo do texto e título dos e-mails de alerta do tipo Compra e do tipo Venda.

6. Tendo em vista que enviar muitos alertas de um mesmo tipo consecutivamente pode facilmente lotar uma caixa de e-mail a depender da taxa de atualização das requisições que foi informada:
    - Implementei condicionais para que não haja dois alertas de compra consecutivos, bem como dois alertas de venda consecutivos (um controle por histerese: o alerta só é rearmado quando o preço retorna à faixa neutra entre as duas referências). Dessa forma um alerta de compra é disparado na primeira requisição cujo valor é menor que o valor de referência de compra, análogo para alertas de venda. Esse comportamento pode ser ajustado a depender das necessidades do usuário final.

7. Por fim, com a aplicação funcional, foram feitos ajustes em comentários, correções de typos e a escrita desse `README.md`.


## Comentários

Como trabalhei nesse projeto sozinho, as etapas de desenvolvimento acabaram se tornando inerentemente sequenciais. Dessa forma todos os pushes foram realizados diretamente na `main`, prática que reconheço como prejudicial, mas foi utilizada devido a ausência de etapas que poderiam ser paralelizadas durante o processo. 

Utilizei a LLM Gemini para apoio pontual em: dúvidas de sintaxe, métodos nativos do .NET para envio de e-mail, tipos de exceção e cenários adequados de uso, e sugestões de APIs gratuitas de cotação.  