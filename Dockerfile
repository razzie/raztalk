FROM mono as builder
WORKDIR /workspace
COPY . .
RUN xbuild /target:raztalk /p:Configuration=Release raztalk.sln

FROM mono
WORKDIR /
COPY --from=builder /workspace/raztalk/bin/Release .
CMD ["mono", "./raztalk.exe", "http://+:8080"]
