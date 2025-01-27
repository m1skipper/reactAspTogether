# Варианты развертывания и тестирования связки react + Asp.Net(api)

## Задача

Создать проект для React.js и связать с проектом по API

**Цель:**

Развернуть ASP.NET Core и React на одном и отдельном хосте.

## Решение 

3 примера взаимодействия фронт и бэк серверов:

+ reactAspProxy 
+ reactAspCors 
+ reactAspOneServer 

## 1) Вариант разработка/отладка(Proxy) 

Проект/папка **reactAspProxy**

1) Проект создан из базового шаблона Visual Studio 2022 "react + TypeScript + Asp.Net". В шаблоне создается два подпроекта "reactapp1.client", "ReactApp1.Server".
2) По умолчанию не работает команда "npm install" для "reactapp1.client". Ошибка, нет требуемой версии "@types/node". 
Для исправления нужно в папке package.json исправить версию на подходящую: "@types/node": "^22.0.0"
3) В файле "vite.config.ts" настраивается проброс запросов к api через npm-proxy сервер. Запросы переправляем на target сервер. 
А для клиента, они будут выглядеть, как будто отдал текущий сервер(nodejs) клиента.

```
const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` : env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7163';

export default defineConfig({
    ...
    server: {
        proxy: {
            '^/weatherforecast': {
                target,
                secure: false
            }
        },
        port: 5173,
	...
    }
})
```

4) В файле reactapp1.client.esproj настраивается как запускать из студии. (Запускается отладочный nodejs сервер(он же будет проксировать запросы на Asp.Net сервер))

```
    <StartupCommand>npm run dev</StartupCommand>
```

5) В проекте ReactApp1.Server со своей стороны подключен модуль Microsoft.AspNetCore.SpaProxy, который также запускает nodejs сервер при необходимости.
В файле ReactApp1.Server.csproj настройка этого сервиса, связь с проектом клиента:

```
    <SpaRoot>..\reactapp1.client</SpaRoot>
    <SpaProxyLaunchCommand>npm run dev</SpaProxyLaunchCommand>
    <SpaProxyServerUrl>https://localhost:5173</SpaProxyServerUrl>
```
6) В файле Properties/launchSettings.json настраивается профиль для запуска и отладки всего проекта.
```
  "profiles": {
    "http": {
      "launchBrowser": true,
      "launchUrl": "weatherforecast",
      "applicationUrl": "http://localhost:5213",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES": "Microsoft.AspNetCore.SpaProxy"
      }
    }
```

## 2) Вариант развертывание - Один сервер

Проект/папка **reactAspOneServer**

Создаем его копией первого проекта.

1) Конфигурируем папку для сборки release проекта клиента в файле "vite.config.ts". 
Собирать будем в папку сервера "ReactApp1.Server/wwwroot" - папка статических файлов сервера по умолчанию.

```
export default defineConfig({
    build: {
       outDir: '../ReactApp1.Server/wwwroot',
       emptyOutDir: true, // also necessary
    }
    ...
})
```
2) Добавляем в папку сервера  файл "ReactApp1.Server\buildClient.bat", для сборки проекта. Запускаем и собираем проект в папку "wwwroot"

```
cd ..\reactapp1.client
npm run build
```

3) Отключаем модуль "Microsoft.AspNetCore.SpaProxy" и убираем его настройки.
Проект ReactApp1.Server более не зависит напрямую от клиента, а просто отдает файлы построенного SPA, как статические страницы(из "wwwroot"). 
nodejs сервер более не нужен. Production ready!

## 3) Вариант кроссдоменное взаимодействие - CORS

Проект/папка **reactAspCors**

1) Берём за основу проект с Proxy, но отключаем proxy в файле "reactapp1.client.esproj" проекта "reactapp1.client"

```
export default defineConfig({
    ...
    /*,
    server: {
        proxy: {
            '^/weatherforecast': {
                target,
                secure: false
            }
        },
        port: 5173,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }*/
})
```

2) Отправляем запросы на прямую на бэкэнд api сервер Asp.Net на другой порт (http://localhost:5213/). Для примера CORS исправляем файл "App.tsx".

```
function App() {
    ...
    const serverUrl = "http://localhost:5213/";
 
    async function populateWeatherData() {
        const response = await fetch(serverUrl + 'weatherforecast');
        const data = await response.json();
        setForecasts(data);
    }
}
```

3) На сервере в проекте "ReactApp1.Server" также отключаем модуль поддержки proxy "Microsoft.AspNetCore.SpaProxy" (ReactApp1.Server.csproj), т.к. у нас не будет прокси, 
а будут кроссдоменные запросы.

4) Настраиваем CORS на сервере, чтобы сервер принимал запросы с другого домена 
(домена клиента, который развёрнут на nodejs сервере "http://localhost:5173")
Для этого в файл Program.cs добавляем регистрацию политик

```

// AddCors
builder.Services.AddCors(options =>
{
    options.AddPolicy("TestPolicy",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod().AllowAnyHeader();
        });
});

builder.Services.AddControllers();
var app = builder.Build();

app.UseCors();

```

5) В файл api контроллера Controllers\WeatherForecastController.cs добавляем атрибут [EnableCors("TestPolicy")],
чтобы разрешить кроссдоменные запросы для этого api

```
namespace ReactApp1.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [EnableCors("TestPolicy")]
    public class WeatherForecastController : ControllerBase {
    ...
```

6) Запускаем, проверяем. Теперь два сервера работают независимо, но клиент может обращаться к api на другом сервере.